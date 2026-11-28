using DebtManagement.Shared.Constants;
using DebtManagement.Shared.DTOs;
using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using static DebtManagement.Application.Services.DebtDocumentRules;

namespace DebtManagement.Application.Services;

public class DebtItemVerificationService(
    IDebtDocumentRepository docs,
    IDepartmentRepository departments,
    IUserRepository users,
    IDocumentAuditService audit) : IDebtItemVerificationService
{
    private async Task TryAutoCompleteDocumentAsync(DebtDocument d, Guid actorUserId, string actorDeptCode, DateTime completedAt)
    {
        if (d.Status != DocumentStatus.PdvEntryApproved)
            return;

        var pkh = await departments.GetByCodeAsync("PKH");
        var pqln = await departments.GetByCodeAsync("PQLN");
        var pdv = await departments.GetByCodeAsync("PDV");

        var latestItems = await docs.GetDebtItemsAsync(d.Id);
        var allCompleted = latestItems.All(x => IsItemCompleted(x, d.WorkflowType, pkh?.Id, pqln?.Id, pdv?.Id));
        if (!allCompleted)
            return;

        d.Status = DocumentStatus.Completed;
        d.CompletedAt = completedAt;
        d.CompletedByUserId = actorUserId;
        await docs.UpdateAsync(d);

        await audit.AddHistoryAsync(
            d.Id,
            d.CurrentDepartmentId,
            d.CurrentDepartmentId,
            CirculationAction.Completed,
            DocumentStatus.Completed,
            actorUserId,
            $"Tự động hoàn tất hồ sơ: Toàn bộ chứng từ nợ đã được bổ sung và xác nhận đủ (xác nhận cuối bởi NV {actorDeptCode})");
    }

    public async Task AddDebtItemsAsync(Guid id, AddDebtItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        var d = await docs.GetDetailByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var pqln = await departments.GetByCodeAsync("PQLN");
        var pdv = await departments.GetByCodeAsync("PDV");

        var isStaff = user.Role == WorkflowConstants.RoleStaff;
        var canAdd = dept != null && (
            (WorkflowConstants.IsSalesDept(dept.Code)
             && isStaff
             && d.Status is DocumentStatus.PkhPendingSup or DocumentStatus.ReturnedToPkh
             && d.CreatedByUserId == u)
            ||
            (dept.Code == WorkflowConstants.DeptPqln
             && isStaff
             && NormalizeWorkflowType(d.WorkflowType) == WorkflowStandard
             && d.Status is DocumentStatus.PqlnReceived or DocumentStatus.PqlnPendingSup or DocumentStatus.ReturnedToPqln
             && d.PqlnReceiverId == u)
        );

        if (!canAdd || dept == null)
            throw new UnauthorizedAccessException("Bạn không có quyền thêm chứng từ nợ ở trạng thái hiện tại của hồ sơ");

        var items = (r.Items ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemName))
            .Select(x =>
            {
                var targetCode = dept.Code == WorkflowConstants.DeptPqln
                    ? WorkflowConstants.DeptPdv
                    : NormalizeTargetDeptCode(x.TargetDepartmentCode, d.WorkflowType);
                var targetDeptId = targetCode == WorkflowConstants.DeptPdv ? pdv?.Id : pqln?.Id;
                return new DebtItem
                {
                    DebtDocumentId = id,
                    ItemName = x.ItemName.Trim(),
                    Notes = x.Notes,
                    SourceDepartmentId = dept.Id,
                    TargetDepartmentId = targetDeptId,
                    CreatedByUserId = u
                };
            }).ToList();

        if (items.Count == 0) throw new InvalidOperationException("Vui lòng nhập ít nhất một chứng từ");

        await docs.AddDebtItemsAsync(items);
        d.HasDebtDocuments = true;
        await docs.UpdateAsync(d);

        var itemsText = string.Join(", ", items.Select(x =>
        {
            var tCode = x.TargetDepartmentId == pdv?.Id ? "PDV" : "PQLN";
            return $"{x.ItemName} [Nợ {tCode}]";
        }));
        var isGenericNote = string.IsNullOrWhiteSpace(r.Notes)
            || r.Notes.Trim().StartsWith("Thêm chứng từ nợ tại", StringComparison.OrdinalIgnoreCase);

        var note = isGenericNote
            ? $"NV {dept.Code} thêm chứng từ nợ: {itemsText}"
            : $"{r.Notes!.Trim()} ({itemsText})";

        await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, note);
    }

    private async Task<(DebtDocument Document, DebtItem Item, Department ActorDept)> GetEditableItemAsync(Guid documentId, Guid itemId, Guid userId)
    {
        var (user, dept) = await GetActorAsync(users, departments, userId);
        var document = await docs.GetDetailByIdAsync(documentId)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var item = document.DebtItems.FirstOrDefault(x => x.Id == itemId)
            ?? throw new InvalidOperationException("Không tìm thấy chứng từ nợ");

        if (item.IsSupplemented)
            throw new InvalidOperationException("Không thể sửa hoặc xóa chứng từ nợ đã được bổ sung");

        var isStaff = user.Role == WorkflowConstants.RoleStaff;
        var allowed = dept != null && (
            (WorkflowConstants.IsSalesDept(dept.Code)
             && isStaff
             && document.Status is DocumentStatus.PkhPendingSup or DocumentStatus.ReturnedToPkh
             && document.CreatedByUserId == userId
             && (item.SourceDepartmentId == dept.Id || item.SourceDepartmentId == null || WorkflowConstants.IsSalesDept(item.SourceDepartment?.Code)))
            ||
            (dept.Code == WorkflowConstants.DeptPqln
             && isStaff
             && NormalizeWorkflowType(document.WorkflowType) == WorkflowStandard
             && document.Status is DocumentStatus.PqlnReceived or DocumentStatus.PqlnPendingSup or DocumentStatus.ReturnedToPqln
             && document.PqlnReceiverId == userId
             && item.SourceDepartmentId == dept.Id)
        );

        if (!allowed || dept == null)
            throw new UnauthorizedAccessException("Bạn không có quyền sửa/xóa chứng từ nợ ở trạng thái hiện tại của hồ sơ");

        return (document, item, dept);
    }

    public async Task UpdateDebtItemAsync(Guid documentId, Guid itemId, UpdateDebtItemRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.ItemName))
            throw new InvalidOperationException("Tên chứng từ không được để trống");

        var (document, item, dept) = await GetEditableItemAsync(documentId, itemId, userId);
        var pqln = await departments.GetByCodeAsync("PQLN");
        var pdv = await departments.GetByCodeAsync("PDV");

        var oldName = item.ItemName;
        item.ItemName = request.ItemName.Trim();
        item.Notes = request.Notes;

        if (!string.IsNullOrWhiteSpace(request.TargetDepartmentCode))
        {
            var targetCode = dept.Code == WorkflowConstants.DeptPqln
                ? WorkflowConstants.DeptPdv
                : NormalizeTargetDeptCode(request.TargetDepartmentCode, document.WorkflowType);
            item.TargetDepartmentId = targetCode == WorkflowConstants.DeptPdv ? pdv?.Id : pqln?.Id;
        }

        await docs.UpdateDebtItemAsync(item);

        await audit.AddHistoryAsync(document.Id, document.CurrentDepartmentId, document.CurrentDepartmentId,
            CirculationAction.Supplemented, document.Status, userId, $"Sửa tên chứng từ nợ: '{oldName}' → '{item.ItemName}'");
    }

    public async Task DeleteDebtItemAsync(Guid documentId, Guid itemId, Guid userId)
    {
        var (document, item, dept) = await GetEditableItemAsync(documentId, itemId, userId);
        await docs.DeleteDebtItemAsync(itemId);

        if (!document.DebtItems.Any(x => x.Id != itemId))
        {
            document.HasDebtDocuments = false;
            await docs.UpdateAsync(document);
        }

        await audit.AddHistoryAsync(document.Id, document.CurrentDepartmentId, document.CurrentDepartmentId,
            CirculationAction.Supplemented, document.Status, userId, $"Xóa chứng từ nợ: {item.ItemName}");

        await TryAutoCompleteDocumentAsync(document, userId, dept.Code, DateTime.UtcNow);
    }

    public async Task SupplementItemsAsync(Guid id, SupplementItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");

        var isStaff = user.Role == WorkflowConstants.RoleStaff;
        var allowed = dept != null && (
            (WorkflowConstants.IsSalesDept(dept.Code) && isStaff && d.CreatedByUserId == u)
            || (dept.Code == WorkflowConstants.DeptPqln && isStaff && NormalizeWorkflowType(d.WorkflowType) == WorkflowStandard && d.PqlnReceiverId == u)
        );
        if (!allowed || dept == null)
            throw new UnauthorizedAccessException("Bạn chỉ được bổ sung chứng từ của hồ sơ do mình phụ trách");

        var requestedIds = (r.SupplementedItemIds ?? []).Distinct().ToList();
        if (requestedIds.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một chứng từ để bổ sung");

        var allItems = await docs.GetDebtItemsAsync(id);
        var targetItems = allItems.Where(x => requestedIds.Contains(x.Id)).ToList();

        bool BelongsToMyDept(DebtItem item) =>
            item.SourceDepartmentId == dept.Id
            || (WorkflowConstants.IsSalesDept(dept.Code) && (item.SourceDepartmentId == null || WorkflowConstants.IsSalesDept(item.SourceDepartment?.Code)));

        if (targetItems.Any(x => !BelongsToMyDept(x)))
            throw new UnauthorizedAccessException($"Nhân viên {dept.Code} không được phép bổ sung chứng từ nợ thuộc phòng ban khác");

        var supplementedNames = new List<string>();
        foreach (var x in targetItems)
        {
            if (x.IsSupplemented) continue;
            x.IsSupplemented = true;
            x.SupplementedAt = DateTime.UtcNow;
            x.SupplementedByUserId = u;
            x.LastRejectReason = null;
            x.RejectedAt = null;
            x.RejectedByUserId = null;
            x.RejectedByUserName = null;
            x.RejectedByDepartmentCode = null;
            await docs.UpdateDebtItemAsync(x);
            supplementedNames.Add(x.ItemName);
        }

        if (supplementedNames.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một chứng từ chưa bổ sung");

        var itemsText = string.Join(", ", supplementedNames);
        var note = string.IsNullOrWhiteSpace(r.Notes)
                   || r.Notes.Trim().Equals("Bổ sung chứng từ nợ", StringComparison.OrdinalIgnoreCase)
            ? $"NV {dept.Code} bổ sung chứng từ nợ: {itemsText}"
            : $"{r.Notes.Trim()} ({itemsText})";

        await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, note);
    }

    public async Task VerifySupplementItemsAsync(Guid id, VerifySupplementItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        if (user.Role != "staff" || dept?.Code is not ("PQLN" or "PDV"))
            throw new UnauthorizedAccessException("Chỉ nhân viên phòng PQLN hoặc PDV mới có quyền xác nhận chứng từ bổ sung.");

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (dept.Code == "PQLN" && d.PqlnReceiverId != u)
            throw new UnauthorizedAccessException("Chỉ cán bộ PQLN đang thụ lý hồ sơ này mới được xác nhận.");
        if (dept.Code == "PDV" && d.PdvReceiverId != u)
            throw new UnauthorizedAccessException("Chỉ cán bộ PDV đang thụ lý hồ sơ này mới được xác nhận.");

        var pkh = await departments.GetByCodeAsync("PKH");
        var pqln = await departments.GetByCodeAsync("PQLN");
        var pdv = await departments.GetByCodeAsync("PDV");
        var docOriginCode = GetOriginDepartmentCode(d);

        var requestedIds = (r.ItemIds ?? []).Distinct().ToList();
        if (requestedIds.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một chứng từ để xác nhận.");

        var targetItems = d.DebtItems.Where(x => requestedIds.Contains(x.Id)).ToList();

        if (targetItems.Any(x => GetTargetDepartmentCode(x, d.WorkflowType, pqln?.Id, pdv?.Id) != dept.Code))
            throw new UnauthorizedAccessException($"Chứng từ đã chọn không thuộc danh sách nợ phòng {dept.Code}.");

        if (targetItems.Any(x => !x.IsSupplemented))
            throw new InvalidOperationException("Không thể xác nhận chứng từ khi phòng nợ chưa tích bổ sung.");

        var verifiedNames = new List<string>();
        var sourceDepts = new HashSet<string>();
        var now = DateTime.UtcNow;

        foreach (var x in targetItems)
        {
            if (dept.Code == "PDV")
            {
                if (x.IsVerifiedByPdv) continue;
                x.IsVerifiedByPdv = true;
                x.PdvVerifiedAt = now;
                x.PdvVerifiedByUserId = u;
            }
            else
            {
                if (x.IsVerifiedByPqln) continue;
                x.IsVerifiedByPqln = true;
                x.PqlnVerifiedAt = now;
                x.PqlnVerifiedByUserId = u;
            }
            await docs.UpdateDebtItemAsync(x);
            verifiedNames.Add(x.ItemName);
            sourceDepts.Add(GetSourceDepartmentCode(x, pkh?.Id, pqln?.Id, docOriginCode));
        }

        if (verifiedNames.Count == 0)
            throw new InvalidOperationException("Các chứng từ đã chọn đều đã được xác nhận trước đó.");

        var itemsText = string.Join(", ", verifiedNames);
        var fromLabel = string.Join("/", sourceDepts);
        var note = string.IsNullOrWhiteSpace(r.Notes)
            ? $"NV {dept.Code} xác nhận đã nhận chứng từ bổ sung từ {fromLabel}: {itemsText}"
            : $"{r.Notes.Trim()} ({itemsText})";

        await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, note);

        await TryAutoCompleteDocumentAsync(d, u, dept.Code, now);
    }

    public Task VerifyPqlnSupplementItemsAsync(Guid id, VerifyPqlnSupplementItemsRequest r, Guid u)
        => VerifySupplementItemsAsync(id, new VerifySupplementItemsRequest(r.ItemIds, r.Notes), u);

    public async Task RejectSupplementItemAsync(Guid documentId, Guid itemId, RejectSupplementItemRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do từ chối chứng từ bổ sung.");

        var (user, dept) = await GetActorAsync(users, departments, userId);
        if (user.Role != "staff" || dept?.Code is not ("PQLN" or "PDV"))
            throw new UnauthorizedAccessException("Chỉ cán bộ chuyên trách đang thụ lý hồ sơ mới được quyền từ chối chứng từ bổ sung.");

        var d = await docs.GetDetailByIdAsync(documentId)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (dept.Code == "PQLN" && d.PqlnReceiverId != userId)
            throw new UnauthorizedAccessException("Chỉ cán bộ PQLN đang thụ lý hồ sơ mới được từ chối.");
        if (dept.Code == "PDV" && d.PdvReceiverId != userId)
            throw new UnauthorizedAccessException("Chỉ cán bộ PDV đang thụ lý hồ sơ mới được từ chối.");

        var item = d.DebtItems.FirstOrDefault(x => x.Id == itemId)
            ?? throw new InvalidOperationException("Không tìm thấy chứng từ nợ.");

        if (!item.IsSupplemented)
            throw new InvalidOperationException("Chứng từ này chưa được đánh dấu bổ sung.");

        var pkh = await departments.GetByCodeAsync("PKH");
        var pqln = await departments.GetByCodeAsync("PQLN");
        var pdv = await departments.GetByCodeAsync("PDV");
        var docOriginCode = GetOriginDepartmentCode(d);

        var targetCode = GetTargetDepartmentCode(item, d.WorkflowType, pqln?.Id, pdv?.Id);
        var alreadyVerified = dept.Code == "PDV" ? item.IsVerifiedByPdv : item.IsVerifiedByPqln;

        if (targetCode != dept.Code || alreadyVerified)
            throw new UnauthorizedAccessException("Bạn không có quyền từ chối chứng từ này hoặc chứng từ đã được xác nhận.");

        var reason = request.Reason.Trim();
        item.IsSupplemented = false;
        item.SupplementedAt = null;
        item.SupplementedByUserId = null;
        item.LastRejectReason = reason;
        item.RejectedAt = DateTime.UtcNow;
        item.RejectedByUserId = userId;
        item.RejectedByUserName = user.Name;
        item.RejectedByDepartmentCode = dept.Code;

        await docs.UpdateDebtItemAsync(item);

        var sourceCode = GetSourceDepartmentCode(item, pkh?.Id, pqln?.Id, docOriginCode);
        await audit.AddHistoryAsync(
            documentId,
            d.CurrentDepartmentId,
            d.CurrentDepartmentId,
            CirculationAction.Returned,
            d.Status,
            userId,
            $"NV {dept.Code} từ chối chứng từ bổ sung '{item.ItemName}' ({sourceCode}): {reason}");
    }
}