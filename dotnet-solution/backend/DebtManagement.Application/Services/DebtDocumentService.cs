using DebtManagement.Shared.Constants;
using DebtManagement.Shared.DTOs;
using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using static DebtManagement.Application.Services.DebtDocumentRules;

namespace DebtManagement.Application.Services;

public class DebtDocumentService(
    IDebtDocumentRepository docs,
    IDepartmentRepository departments,
    IUserRepository users,
    IDocumentAuditService audit,
    IDebtWorkflowService workflow,
    IDebtItemVerificationService itemsService) : IDebtDocumentService
{
    public const string WorkflowStandard = DebtDocumentRules.WorkflowStandard;
    public const string WorkflowDirectPdv = DebtDocumentRules.WorkflowDirectPdv;

    public async Task<IReadOnlyList<DebtDocumentDto>> GetAllAsync(
        string? status, Guid? deptId, string? search, Guid currentUserId)
    {
        var (user, dept) = await GetActorAsync(users, departments, currentUserId);
        var pkh = await departments.GetByCodeAsync("PKH");
        var pqln = await departments.GetByCodeAsync("PQLN");
        var pdv = await departments.GetByCodeAsync("PDV");
        var all = await docs.GetAllAsync(status, deptId, search);
        var visible = all.Where(d => CanView(user, dept, d)).ToList();
        return visible.Select(d => Map(d, pkh?.Id, pqln?.Id, pdv?.Id, user, dept?.Code)).ToList();
    }

    public async Task<DebtDocumentDetailDto?> GetByIdAsync(Guid id, Guid currentUserId)
    {
        var (user, dept) = await GetActorAsync(users, departments, currentUserId);
        var d = await docs.GetDetailByIdAsync(id);
        if (d == null) return null;
        if (!CanView(user, dept, d))
            throw new UnauthorizedAccessException("Bạn không có quyền xem hồ sơ này");

        var pkh = await departments.GetByCodeAsync("PKH");
        var pqln = await departments.GetByCodeAsync("PQLN");
        var pdv = await departments.GetByCodeAsync("PDV");

        var originDept = GetOriginDepartment(d);
        var originCode = GetOriginDepartmentCode(d);
        var originName = originDept?.Name ?? WorkflowConstants.GetDepartmentDisplayName(originCode);

        var visibleItems = new List<DebtItem>();
        var hiddenItemNames = new List<string>();

        foreach (var x in d.DebtItems)
        {
            if (CanViewDebtItem(user, dept?.Code, x, d.WorkflowType, pkh?.Id, pqln?.Id, pdv?.Id, originCode))
            {
                visibleItems.Add(x);
            }
            else if (!string.IsNullOrWhiteSpace(x.ItemName))
            {
                hiddenItemNames.Add(x.ItemName.Trim());
            }
        }

        var verifierIds = visibleItems
            .SelectMany(x => new[] { x.PqlnVerifiedByUserId, x.PdvVerifiedByUserId })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct();

        var userNameCache = new Dictionary<Guid, string?>();
        foreach (var verifierId in verifierIds)
        {
            var verifier = await users.GetByIdAsync(verifierId);
            userNameCache[verifierId] = verifier?.Name;
        }

        var mappedItems = visibleItems.Select(x =>
        {
            string? pqlnVerifierName = x.PqlnVerifiedByUserId.HasValue && userNameCache.TryGetValue(x.PqlnVerifiedByUserId.Value, out var pqlnName)
                ? pqlnName
                : null;
            string? pdvVerifierName = x.PdvVerifiedByUserId.HasValue && userNameCache.TryGetValue(x.PdvVerifiedByUserId.Value, out var pdvName)
                ? pdvName
                : null;
            var targetCode = GetTargetDepartmentCode(x, d.WorkflowType, pqln?.Id, pdv?.Id);
            var targetName = targetCode == "PDV" ? pdv?.Name : pqln?.Name;
            return Item(x, pqlnVerifierName, pdvVerifierName, targetCode, targetName);
        }).ToList();

        var distinctHiddenNames = hiddenItemNames
            .OrderByDescending(n => n.Length)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var historyList = (await docs.GetHistoryAsync(id)).Select(h =>
        {
            var maskedNote = MaskHiddenNotes(h.Notes, distinctHiddenNames);
            return Hist(h, maskedNote);
        }).ToList();

        return new(
            d.Id, d.DocumentNumber, d.CustomerName, d.CustomerCif,
            NormalizeCustomerType(d.CustomerType),
            NormalizeWorkflowType(d.WorkflowType),
            d.IssueDate.ToString("yyyy-MM-dd"), d.DueDate.ToString("yyyy-MM-dd"),
            d.CurrentDepartmentId, d.CurrentDepartment?.Name, d.CurrentDepartment?.Code,
            d.Status.ToString(), d.Priority.ToString().ToLower(),
            d.Notes, d.HasDebtDocuments,
            d.CreatedByUserId, d.CreatedByUser?.Name, d.CreatedAt,
            d.SupPkhApprovedAt, d.SupPkhApproverId, null,
            d.PqlnReceiverId, d.PqlnReceiver?.Name, d.PqlnReceivedAt,
            d.SupPqlnApprovedAt, d.SupPqlnApproverId, null,
            d.PdvReceiverId, d.PdvReceiver?.Name, d.PdvReceivedAt,
            d.EntryPostedAt, d.EntryPostedById, null,
            d.SupPdvApprovedEntryAt, d.SupPdvEntryApproverId, null,
            d.CompletedAt, d.CompletedByUserId, null,
            d.UpdatedAt,
            mappedItems,
            historyList,
            (await docs.GetEditLogsAsync(id)).Select(Log).ToList(),
            d.ReturnReason, d.ReturnedAt, d.ReturnedByUserId, d.ReturnedByUserName, d.ReturnedFromDepartmentCode,
            hiddenItemNames.Count,
            originCode, originName);
    }

    private static string? MaskHiddenNotes(string? notes, IReadOnlyList<string> hiddenNames)
    {
        if (string.IsNullOrWhiteSpace(notes) || hiddenNames.Count == 0)
            return notes;

        var result = notes;
        foreach (var name in hiddenNames)
        {
            result = result.Replace(name, "[Chứng từ nợ đã ẩn]", StringComparison.OrdinalIgnoreCase);
        }
        return result;
    }

    public async Task<DebtDocumentDto> CreateAsync(CreateDebtDocumentRequest r, Guid uid)
    {
        var (user, salesDept) = await GetActorAsync(users, departments, uid);
        if (salesDept == null || !WorkflowConstants.IsSalesDept(salesDept.Code) || user.Role != WorkflowConstants.RoleStaff)
            throw new UnauthorizedAccessException("Chỉ nhân viên thuộc khối bán hàng (KHDN, KHBL, PGD LLQ, PGD Cửa Đông) mới được khởi tạo hồ sơ.");

        var pkh = await departments.GetByCodeAsync("PKH");
        var pqln = await departments.GetByCodeAsync("PQLN");
        var pdv = await departments.GetByCodeAsync("PDV");
        var nowVn = DateTime.UtcNow.AddHours(7);
        var workflowType = NormalizeWorkflowType(r.WorkflowType);

        var d = new DebtDocument
        {
            DocumentNumber = $"VB-{nowVn:yyyy-MM-dd}-{Random.Shared.Next(100000, 1000000)}",
            CustomerName = r.CustomerName.Trim(),
            CustomerCif = string.IsNullOrWhiteSpace(r.CustomerCif) ? null : r.CustomerCif.Trim(),
            CustomerType = NormalizeCustomerType(r.CustomerType),
            WorkflowType = workflowType,
            IssueDate = DateOnly.FromDateTime(nowVn),
            DueDate = DateOnly.Parse(r.DueDate),
            CurrentDepartmentId = salesDept.Id,
            Status = DocumentStatus.PkhPendingSup,
            Priority = Enum.TryParse<DocumentPriority>(r.Priority, true, out var parsedPriority) ? parsedPriority : DocumentPriority.Normal,
            Notes = r.Notes,
            CreatedByUserId = uid
        };

        var debtItems = r.HasDebtDocuments
            ? (r.DebtItems ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x.ItemName))
                .Select(x =>
                {
                    var targetCode = NormalizeTargetDeptCode(x.TargetDepartmentCode, workflowType);
                    var targetDeptId = targetCode == "PDV" ? pdv?.Id : pqln?.Id;
                    return new DebtItem
                    {
                        DebtDocumentId = d.Id,
                        ItemName = x.ItemName.Trim(),
                        Notes = x.Notes,
                        SourceDepartmentId = salesDept.Id,
                        TargetDepartmentId = targetDeptId,
                        CreatedByUserId = uid
                    };
                }).ToList()
            : [];

        d.HasDebtDocuments = debtItems.Count > 0;
        await docs.AddAsync(d);

        if (debtItems.Count > 0)
            await docs.AddDebtItemsAsync(debtItems);

        var flowTag = workflowType == WorkflowDirectPdv ? " [Luồng chuyển thẳng PDV]" : "";
        var itemsSummary = debtItems.Select(x =>
        {
            var tCode = x.TargetDepartmentId == pdv?.Id ? "PDV" : "PQLN";
            return $"{x.ItemName} (Nợ {tCode})";
        });
        var createNote = debtItems.Count > 0
            ? $"Khởi tạo hồ sơ{flowTag} tại {salesDept.Name} ({salesDept.Code}) (Chứng từ nợ: {string.Join(", ", itemsSummary)}) và gửi SUP {salesDept.Code}"
            : $"Khởi tạo hồ sơ{flowTag} tại {salesDept.Name} ({salesDept.Code}) và gửi SUP {salesDept.Code}";

        await audit.AddHistoryAsync(d.Id, null, salesDept.Id, CirculationAction.Created,
            DocumentStatus.PkhPendingSup, uid, createNote);

        return Map((await docs.GetByIdAsync(d.Id))!, pkh?.Id, pqln?.Id, pdv?.Id, user, salesDept.Code);
    }

    public async Task<DebtDocumentDto> UpdateAsync(Guid id, UpdateDebtDocumentRequest r, Guid uid)
    {
        var (user, dept) = await GetActorAsync(users, departments, uid);
        var pkh = await departments.GetByCodeAsync("PKH");
        var pqln = await departments.GetByCodeAsync("PQLN");
        var pdv = await departments.GetByCodeAsync("PDV");
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        if (!CanEdit(user, dept, d))
            throw new UnauthorizedAccessException("Bạn chỉ được sửa hồ sơ do chính mình khởi tạo hoặc tiếp nhận");

        d.CustomerName = r.CustomerName.Trim();
        d.CustomerCif = string.IsNullOrWhiteSpace(r.CustomerCif) ? null : r.CustomerCif.Trim();
        d.CustomerType = NormalizeCustomerType(r.CustomerType);
        if (d.Status is DocumentStatus.PkhPendingSup or DocumentStatus.ReturnedToPkh)
            d.WorkflowType = NormalizeWorkflowType(r.WorkflowType);
        d.DueDate = DateOnly.Parse(r.DueDate);
        d.Priority = Enum.TryParse<DocumentPriority>(r.Priority, true, out var parsedPriority) ? parsedPriority : DocumentPriority.Normal;
        d.Notes = r.Notes;
        d.HasDebtDocuments = r.HasDebtDocuments;

        if (r.DebtItems != null)
        {
            await docs.RemoveDebtItemsAsync(id);
            if (r.HasDebtDocuments)
            {
                await docs.AddDebtItemsAsync(r.DebtItems
                    .Where(x => !string.IsNullOrWhiteSpace(x.ItemName))
                    .Select(x =>
                    {
                        var targetCode = dept?.Code == "PQLN"
                            ? "PDV"
                            : NormalizeTargetDeptCode(x.TargetDepartmentCode, d.WorkflowType);
                        var targetDeptId = targetCode == "PDV" ? pdv?.Id : pqln?.Id;
                        return new DebtItem
                        {
                            DebtDocumentId = id,
                            ItemName = x.ItemName.Trim(),
                            Notes = x.Notes,
                            SourceDepartmentId = dept?.Id,
                            TargetDepartmentId = targetDeptId,
                            CreatedByUserId = uid
                        };
                    }));
            }
        }

        await docs.UpdateAsync(d);
        return Map((await docs.GetByIdAsync(id))!, pkh?.Id, pqln?.Id, pdv?.Id, user, dept?.Code);
    }

    public Task DeleteAsync(Guid id) => docs.DeleteAsync(id);

    public Task PkhSupApproveAsync(Guid id, string? n, Guid u) => workflow.PkhSupApproveAsync(id, n, u);
    public Task PqlnTakeOwnershipAsync(Guid id, Guid u) => workflow.PqlnTakeOwnershipAsync(id, u);
    public Task PqlnSubmitSupAsync(Guid id, string? n, Guid u) => workflow.PqlnSubmitSupAsync(id, n, u);
    public Task PqlnSupApproveAsync(Guid id, string? n, Guid u) => workflow.PqlnSupApproveAsync(id, n, u);
    public Task PdvTakeOwnershipAsync(Guid id, Guid u) => workflow.PdvTakeOwnershipAsync(id, u);
    public Task PdvPostEntryAsync(Guid id, string? n, Guid u) => workflow.PdvPostEntryAsync(id, n, u);
    public Task PdvSupApproveAsync(Guid id, string? n, Guid u) => workflow.PdvSupApproveAsync(id, n, u);
    public Task ReturnAsync(Guid id, Guid to, string? n, Guid u) => workflow.ReturnAsync(id, to, n, u);

    public Task AddDebtItemsAsync(Guid id, AddDebtItemsRequest r, Guid u) => itemsService.AddDebtItemsAsync(id, r, u);
    public Task UpdateDebtItemAsync(Guid documentId, Guid itemId, UpdateDebtItemRequest request, Guid userId) => itemsService.UpdateDebtItemAsync(documentId, itemId, request, userId);
    public Task DeleteDebtItemAsync(Guid documentId, Guid itemId, Guid userId) => itemsService.DeleteDebtItemAsync(documentId, itemId, userId);
    public Task SupplementItemsAsync(Guid id, SupplementItemsRequest r, Guid u) => itemsService.SupplementItemsAsync(id, r, u);
    public Task VerifySupplementItemsAsync(Guid id, VerifySupplementItemsRequest r, Guid u) => itemsService.VerifySupplementItemsAsync(id, r, u);
    public Task VerifyPqlnSupplementItemsAsync(Guid id, VerifyPqlnSupplementItemsRequest r, Guid u) => itemsService.VerifyPqlnSupplementItemsAsync(id, r, u);
    public Task ReturnByCodeAsync(Guid id, string toDepartmentCode, string? n, Guid u) => workflow.ReturnByCodeAsync(id, toDepartmentCode, n, u);
    public Task ResubmitPkhAsync(Guid id, string? n, Guid u) => workflow.ResubmitPkhAsync(id, n, u);
    public Task RejectSupplementItemAsync(Guid documentId, Guid itemId, RejectSupplementItemRequest request, Guid userId) => itemsService.RejectSupplementItemAsync(documentId, itemId, request, userId);
}