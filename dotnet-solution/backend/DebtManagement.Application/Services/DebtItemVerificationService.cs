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
    public async Task AddDebtItemsAsync(Guid id, AddDebtItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        var d = await docs.GetDetailByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");

        var isStaff = user.Role == "staff";
        var canAdd = dept?.Code switch
        {
            "PKH" => isStaff
                     && d.Status is DocumentStatus.PkhPendingSup or DocumentStatus.ReturnedToPkh
                     && d.CreatedByUserId == u,

            "PQLN" => isStaff
                      && NormalizeWorkflowType(d.WorkflowType) == WorkflowStandard
                      && d.Status is DocumentStatus.PqlnReceived or DocumentStatus.PqlnPendingSup or DocumentStatus.ReturnedToPqln
                      && d.PqlnReceiverId == u,

            _ => false
        };

        if (!canAdd || dept == null)
            throw new UnauthorizedAccessException("Bạn không có quyền thêm chứng từ nợ ở trạng thái hiện tại của hồ sơ");

        var items = (r.Items ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemName))
            .Select(x => new DebtItem
            {
                DebtDocumentId = id,
                ItemName = x.ItemName.Trim(),
                Notes = x.Notes,
                SourceDepartmentId = dept.Id,
                CreatedByUserId = u
            }).ToList();

        if (items.Count == 0) throw new InvalidOperationException("Vui lòng nhập ít nhất một chứng từ");

        await docs.AddDebtItemsAsync(items);
        d.HasDebtDocuments = true;
        await docs.UpdateAsync(d);

        var itemsText = string.Join(", ", items.Select(x => x.ItemName));
        var isGenericNote = string.IsNullOrWhiteSpace(r.Notes)
            || r.Notes.Trim().StartsWith("Thêm chứng từ nợ tại", StringComparison.OrdinalIgnoreCase);

        var note = isGenericNote
            ? $"NV {dept.Code} thêm chứng từ nợ: {itemsText}"
            : $"{r.Notes!.Trim()} ({itemsText})";

        await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, note);
    }

    private async Task<(DebtDocument Document, DebtItem Item)> GetEditableItemAsync(Guid documentId, Guid itemId, Guid userId)
    {
        var (user, dept) = await GetActorAsync(users, departments, userId);
        var document = await docs.GetDetailByIdAsync(documentId)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var item = document.DebtItems.FirstOrDefault(x => x.Id == itemId)
            ?? throw new InvalidOperationException("Không tìm thấy chứng từ nợ");

        if (item.IsSupplemented)
            throw new InvalidOperationException("Không thể sửa hoặc xóa chứng từ nợ đã được bổ sung");

        var isStaff = user.Role == "staff";
        var allowed = dept?.Code switch
        {
            "PKH" => isStaff
                     && document.Status is DocumentStatus.PkhPendingSup or DocumentStatus.ReturnedToPkh
                     && document.CreatedByUserId == userId
                     && item.SourceDepartmentId == dept.Id,

            "PQLN" => isStaff
                      && NormalizeWorkflowType(document.WorkflowType) == WorkflowStandard
                      && document.Status is DocumentStatus.PqlnReceived or DocumentStatus.PqlnPendingSup or DocumentStatus.ReturnedToPqln
                      && document.PqlnReceiverId == userId
                      && item.SourceDepartmentId == dept.Id,

            _ => false
        };

        if (!allowed)
            throw new UnauthorizedAccessException("Bạn không có quyền sửa/xóa chứng từ nợ ở trạng thái hiện tại của hồ sơ");

        return (document, item);
    }

    public async Task UpdateDebtItemAsync(Guid documentId, Guid itemId, UpdateDebtItemRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.ItemName))
            throw new InvalidOperationException("Tên chứng từ không được để trống");

        var (document, item) = await GetEditableItemAsync(documentId, itemId, userId);
        var oldName = item.ItemName;
        item.ItemName = request.ItemName.Trim();
        item.Notes = request.Notes;
        await docs.UpdateDebtItemAsync(item);

        await audit.AddHistoryAsync(document.Id, document.CurrentDepartmentId, document.CurrentDepartmentId,
            CirculationAction.Supplemented, document.Status, userId, $"Sửa tên chứng từ nợ: '{oldName}' → '{item.ItemName}'");
    }

    public async Task DeleteDebtItemAsync(Guid documentId, Guid itemId, Guid userId)
    {
        var (document, item) = await GetEditableItemAsync(documentId, itemId, userId);
        await docs.DeleteDebtItemAsync(itemId);

        if (!document.DebtItems.Any(x => x.Id != itemId))
        {
            document.HasDebtDocuments = false;
            await docs.UpdateAsync(document);
        }

        await audit.AddHistoryAsync(document.Id, document.CurrentDepartmentId, document.CurrentDepartmentId,
            CirculationAction.Supplemented, document.Status, userId, $"Xóa chứng từ nợ: {item.ItemName}");
    }

    public async Task SupplementItemsAsync(Guid id, SupplementItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");

        var isStaff = user.Role == "staff";
        var allowed = dept?.Code switch
        {
            "PKH" => isStaff && d.CreatedByUserId == u,
            "PQLN" => isStaff && NormalizeWorkflowType(d.WorkflowType) == WorkflowStandard && d.PqlnReceiverId == u,
            _ => false
        };
        if (!allowed || dept == null)
            throw new UnauthorizedAccessException("Bạn chỉ được bổ sung chứng từ của hồ sơ do mình phụ trách");

        var requestedIds = (r.SupplementedItemIds ?? []).Distinct().ToList();
        if (requestedIds.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một chứng từ để bổ sung");

        var allItems = await docs.GetDebtItemsAsync(id);
        var targetItems = allItems.Where(x => requestedIds.Contains(x.Id)).ToList();

        bool BelongsToMyDept(DebtItem item) =>
            item.SourceDepartmentId == dept.Id
            || (dept.Code == "PKH" && item.SourceDepartmentId == null);

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
        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        var isDirectPdv = NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv;

        if (isDirectPdv)
        {
            if (dept?.Code != "PDV" || user.Role != "staff")
                throw new UnauthorizedAccessException("Chỉ nhân viên phòng PDV mới có quyền xác nhận chứng từ bổ sung từ PKH cho luồng này.");
            if (d.PdvReceiverId != u)
                throw new UnauthorizedAccessException("Chỉ cán bộ PDV đang thụ lý hồ sơ này mới được xác nhận.");
        }
        else
        {
            if (dept?.Code != "PQLN" || user.Role != "staff")
                throw new UnauthorizedAccessException("Chỉ nhân viên phòng PQLN mới có quyền xác nhận chứng từ bổ sung từ PKH.");
            if (d.PqlnReceiverId != u)
                throw new UnauthorizedAccessException("Chỉ cán bộ PQLN đang thụ lý hồ sơ này mới được xác nhận.");
        }

        var pkh = await departments.GetByCodeAsync("PKH")
            ?? throw new InvalidOperationException("Không tìm thấy phòng ban PKH.");

        var requestedIds = (r.ItemIds ?? []).Distinct().ToList();
        if (requestedIds.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một chứng từ để xác nhận.");

        var targetItems = d.DebtItems.Where(x => requestedIds.Contains(x.Id)).ToList();

        if (targetItems.Any(x => x.SourceDepartmentId != null && x.SourceDepartmentId != pkh.Id))
            throw new InvalidOperationException("Chỉ thực hiện xác nhận đối với chứng từ nợ thuộc phòng PKH.");

        if (targetItems.Any(x => !x.IsSupplemented))
            throw new InvalidOperationException("Không thể xác nhận chứng từ mà nhân viên PKH chưa tích bổ sung.");

        var verifiedNames = new List<string>();
        var now = DateTime.UtcNow;
        foreach (var x in targetItems)
        {
            if (isDirectPdv)
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
        }

        if (verifiedNames.Count == 0)
            throw new InvalidOperationException("Các chứng từ đã chọn đều đã được xác nhận trước đó.");

        var itemsText = string.Join(", ", verifiedNames);
        var verifierDeptCode = isDirectPdv ? "PDV" : "PQLN";
        var note = string.IsNullOrWhiteSpace(r.Notes)
            ? $"NV {verifierDeptCode} xác nhận đã nhận chứng từ bổ sung từ PKH: {itemsText}"
            : $"{r.Notes.Trim()} ({itemsText})";

        await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, note);
    }

    public async Task VerifyPqlnSupplementItemsAsync(Guid id, VerifyPqlnSupplementItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        if (dept?.Code != "PDV" || user.Role != "staff")
            throw new UnauthorizedAccessException("Chỉ nhân viên phòng PDV mới có quyền xác nhận chứng từ bổ sung từ PQLN.");

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv)
            throw new InvalidOperationException("Hồ sơ luồng chuyển thẳng PDV không có chứng từ từ PQLN.");

        if (d.PdvReceiverId != u)
            throw new UnauthorizedAccessException("Chỉ cán bộ PDV đang thụ lý hồ sơ này mới được xác nhận.");

        var pqln = await departments.GetByCodeAsync("PQLN")
            ?? throw new InvalidOperationException("Không tìm thấy phòng ban PQLN.");

        var requestedIds = (r.ItemIds ?? []).Distinct().ToList();
        if (requestedIds.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một chứng từ để xác nhận.");

        var targetItems = d.DebtItems.Where(x => requestedIds.Contains(x.Id)).ToList();

        if (targetItems.Any(x => x.SourceDepartmentId != pqln.Id))
            throw new InvalidOperationException("Chỉ thực hiện xác nhận đối với chứng từ nợ thuộc phòng PQLN.");

        if (targetItems.Any(x => !x.IsSupplemented))
            throw new InvalidOperationException("Không thể xác nhận chứng từ mà nhân viên PQLN chưa tích bổ sung.");

        var verifiedNames = new List<string>();
        var now = DateTime.UtcNow;
        foreach (var x in targetItems)
        {
            if (x.IsVerifiedByPdv) continue;
            x.IsVerifiedByPdv = true;
            x.PdvVerifiedAt = now;
            x.PdvVerifiedByUserId = u;
            await docs.UpdateDebtItemAsync(x);
            verifiedNames.Add(x.ItemName);
        }

        if (verifiedNames.Count == 0)
            throw new InvalidOperationException("Các chứng từ đã chọn đều đã được xác nhận trước đó.");

        var itemsText = string.Join(", ", verifiedNames);
        var note = string.IsNullOrWhiteSpace(r.Notes)
            ? $"NV PDV xác nhận đã nhận chứng từ bổ sung từ PQLN: {itemsText}"
            : $"{r.Notes.Trim()} ({itemsText})";

        await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, note);
    }
    public async Task RejectSupplementItemAsync(Guid documentId, Guid itemId, RejectSupplementItemRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do từ chối chứng từ bổ sung.");

        var (user, dept) = await GetActorAsync(users, departments, userId);
        if (user.Role != "staff" || dept == null)
            throw new UnauthorizedAccessException("Chỉ cán bộ chuyên trách đang thụ lý hồ sơ mới được quyền từ chối chứng từ bổ sung.");

        var d = await docs.GetDetailByIdAsync(documentId)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        var item = d.DebtItems.FirstOrDefault(x => x.Id == itemId)
            ?? throw new InvalidOperationException("Không tìm thấy chứng từ nợ.");

        if (!item.IsSupplemented)
            throw new InvalidOperationException("Chứng từ này chưa được đánh dấu bổ sung.");

        var isDirectPdv = NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv;
        var pkh = await departments.GetByCodeAsync("PKH");
        var pqln = await departments.GetByCodeAsync("PQLN");

        var isPkhItem = item.SourceDepartmentId == null || (pkh != null && item.SourceDepartmentId == pkh.Id);
        var isPqlnItem = pqln != null && item.SourceDepartmentId == pqln.Id;

        bool allowed;
        if (isPkhItem)
        {
            if (isDirectPdv)
            {
                allowed = dept.Code == "PDV" && d.PdvReceiverId == userId && !item.IsVerifiedByPdv;
            }
            else
            {
                allowed = dept.Code == "PQLN" && d.PqlnReceiverId == userId && !item.IsVerifiedByPqln;
            }
        }
        else if (isPqlnItem && !isDirectPdv)
        {
            allowed = dept.Code == "PDV" && d.PdvReceiverId == userId && !item.IsVerifiedByPdv;
        }
        else
        {
            allowed = false;
        }

        if (!allowed)
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

        var sourceCode = isPkhItem ? "PKH" : "PQLN";
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