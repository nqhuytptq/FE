using DebtManagement.Shared.Constants;
using DebtManagement.Shared.DTOs;
using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;

namespace DebtManagement.Application.Services;

internal static class DebtDocumentRules
{
    public const string WorkflowStandard = WorkflowConstants.WorkflowStandard;
    public const string WorkflowDirectPdv = WorkflowConstants.WorkflowDirectPdv;

    public static async Task<(AppUser User, Department? Dept)> GetActorAsync(
        IUserRepository users,
        IDepartmentRepository departments,
        Guid userId)
    {
        var user = await users.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Không tìm thấy người dùng");
        var dept = user.DepartmentId.HasValue
            ? await departments.GetByIdAsync(user.DepartmentId.Value)
            : null;
        return (user, dept);
    }

    public static Department? GetOriginDepartment(DebtDocument d)
    {
        if (d.CreatedByUser?.Department != null && WorkflowConstants.IsSalesDept(d.CreatedByUser.Department.Code))
            return d.CreatedByUser.Department;

        if (d.CurrentDepartment != null && WorkflowConstants.IsSalesDept(d.CurrentDepartment.Code))
            return d.CurrentDepartment;

        var itemDept = d.DebtItems
            .Select(i => i.SourceDepartment)
            .FirstOrDefault(dept => dept != null && WorkflowConstants.IsSalesDept(dept.Code));
        if (itemDept != null)
            return itemDept;

        return d.CreatedByUser?.Department ?? d.CurrentDepartment;
    }

    public static string GetOriginDepartmentCode(DebtDocument d)
    {
        var dept = GetOriginDepartment(d);
        if (!string.IsNullOrWhiteSpace(dept?.Code) && WorkflowConstants.IsSalesDept(dept.Code))
            return dept.Code.Trim().ToUpperInvariant();
        return WorkflowConstants.DeptPkh;
    }

    public static string GetSourceDepartmentCode(DebtItem x, Guid? pkhDeptId = null, Guid? pqlnDeptId = null, string? fallbackSalesCode = null)
    {
        if (!string.IsNullOrWhiteSpace(x.SourceDepartment?.Code))
            return x.SourceDepartment.Code.Trim().ToUpperInvariant();
        if (pqlnDeptId.HasValue && x.SourceDepartmentId == pqlnDeptId.Value)
            return WorkflowConstants.DeptPqln;
        return !string.IsNullOrWhiteSpace(fallbackSalesCode)
            ? fallbackSalesCode.Trim().ToUpperInvariant()
            : WorkflowConstants.DeptPkh;
    }

    public static string GetTargetDepartmentCode(DebtItem x, string workflowType, Guid? pqlnDeptId = null, Guid? pdvDeptId = null)
    {
        if (!string.IsNullOrWhiteSpace(x.TargetDepartment?.Code))
            return x.TargetDepartment.Code.Trim().ToUpperInvariant();

        if (x.TargetDepartmentId.HasValue)
        {
            if (pdvDeptId.HasValue && x.TargetDepartmentId.Value == pdvDeptId.Value) return WorkflowConstants.DeptPdv;
            if (pqlnDeptId.HasValue && x.TargetDepartmentId.Value == pqlnDeptId.Value) return WorkflowConstants.DeptPqln;
        }

        if (NormalizeWorkflowType(workflowType) == WorkflowDirectPdv)
            return WorkflowConstants.DeptPdv;

        var srcCode = GetSourceDepartmentCode(x, null, pqlnDeptId);
        return srcCode == WorkflowConstants.DeptPqln ? WorkflowConstants.DeptPdv : WorkflowConstants.DeptPqln;
    }

    public static bool CanViewDebtItem(
        AppUser user,
        string? viewerDeptCode,
        DebtItem item,
        string workflowType,
        Guid? pkhDeptId = null,
        Guid? pqlnDeptId = null,
        Guid? pdvDeptId = null,
        string? docOriginSalesCode = null)
    {
        if (string.Equals(user.Role, WorkflowConstants.RoleAdmin, StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(viewerDeptCode))
            return false;

        var viewer = viewerDeptCode.Trim().ToUpperInvariant();
        var source = GetSourceDepartmentCode(item, pkhDeptId, pqlnDeptId, docOriginSalesCode);
        var target = GetTargetDepartmentCode(item, workflowType, pqlnDeptId, pdvDeptId);

        if (viewer == source || viewer == target)
            return true;

        // Tương thích dữ liệu lịch sử gắn mã PKH chung khi cán bộ đã chuyển sang 1 trong 4 phòng bán hàng
        if (source == WorkflowConstants.DeptPkh
            && WorkflowConstants.IsSalesDept(viewer)
            && string.Equals(docOriginSalesCode, viewer, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool IsItemCompleted(DebtItem x, string workflowType, Guid? pkhDeptId = null, Guid? pqlnDeptId = null, Guid? pdvDeptId = null)
    {
        if (!x.IsSupplemented) return false;

        var targetCode = GetTargetDepartmentCode(x, workflowType, pqlnDeptId, pdvDeptId);
        return targetCode switch
        {
            WorkflowConstants.DeptPdv => x.IsVerifiedByPdv,
            WorkflowConstants.DeptPqln => x.IsVerifiedByPqln,
            _ => true
        };
    }

    public static int SupplementedCount(DebtDocument d, Guid? pkhDeptId = null, Guid? pqlnDeptId = null, Guid? pdvDeptId = null)
        => d.DebtItems.Count(x => IsItemCompleted(x, d.WorkflowType, pkhDeptId, pqlnDeptId, pdvDeptId));

    public static string NormalizeCustomerType(string? raw)
        => string.Equals(raw?.Trim(), "individual", StringComparison.OrdinalIgnoreCase)
            ? "individual"
            : "company";

    public static string NormalizeWorkflowType(string? raw)
        => string.Equals(raw?.Trim(), WorkflowDirectPdv, StringComparison.OrdinalIgnoreCase)
            ? WorkflowDirectPdv
            : WorkflowStandard;

    public static string NormalizeTargetDeptCode(string? raw, string workflowType)
    {
        if (NormalizeWorkflowType(workflowType) == WorkflowDirectPdv)
            return WorkflowConstants.DeptPdv;
        return string.Equals(raw?.Trim(), WorkflowConstants.DeptPdv, StringComparison.OrdinalIgnoreCase)
            ? WorkflowConstants.DeptPdv
            : WorkflowConstants.DeptPqln;
    }

    public static bool CanView(AppUser user, Department? dept, DebtDocument d)
    {
        if (user.Role == WorkflowConstants.RoleAdmin) return true;

        var code = dept?.Code?.Trim().ToUpperInvariant();
        var wf = NormalizeWorkflowType(d.WorkflowType);

        if (code == WorkflowConstants.DeptPqln && wf == WorkflowDirectPdv)
            return false;

        if (user.Role == WorkflowConstants.RoleManager)
        {
            if (code == WorkflowConstants.DeptPqln)
            {
                return d.CurrentDepartmentId == dept?.Id
                    || d.Status is DocumentStatus.PqlnPendingReceive
                        or DocumentStatus.PqlnReceived
                        or DocumentStatus.PqlnPendingSup
                        or DocumentStatus.ReturnedToPqln
                        or DocumentStatus.PdvPendingReceive
                        or DocumentStatus.PdvReceived
                        or DocumentStatus.PdvEntryPosted
                        or DocumentStatus.PdvEntryApproved
                        or DocumentStatus.Completed;
            }

            if (code == WorkflowConstants.DeptPdv)
            {
                return d.CurrentDepartmentId == dept?.Id
                    || d.Status is DocumentStatus.PdvPendingReceive
                        or DocumentStatus.PdvReceived
                        or DocumentStatus.PdvEntryPosted
                        or DocumentStatus.PdvEntryApproved
                        or DocumentStatus.Completed;
            }

            if (WorkflowConstants.IsSalesDept(code))
            {
                var originCode = GetOriginDepartmentCode(d);
                return string.Equals(originCode, code, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        if (WorkflowConstants.IsSalesDept(code)) return d.CreatedByUserId == user.Id;
        if (code == WorkflowConstants.DeptPqln) return (d.CurrentDepartmentId == dept?.Id && d.Status == DocumentStatus.PqlnPendingReceive) || d.PqlnReceiverId == user.Id;
        if (code == WorkflowConstants.DeptPdv) return (d.CurrentDepartmentId == dept?.Id && d.Status == DocumentStatus.PdvPendingReceive) || d.PdvReceiverId == user.Id;
        return false;
    }

    public static bool CanEdit(AppUser user, Department? dept, DebtDocument d)
    {
        if (user.Role is WorkflowConstants.RoleAdmin or WorkflowConstants.RoleManager) return false;
        var code = dept?.Code;
        if (WorkflowConstants.IsSalesDept(code))
            return d.CreatedByUserId == user.Id
                && d.CurrentDepartmentId == dept!.Id
                && d.Status is DocumentStatus.PkhPendingSup or DocumentStatus.ReturnedToPkh;
        if (code == WorkflowConstants.DeptPqln && NormalizeWorkflowType(d.WorkflowType) == WorkflowStandard)
            return d.CurrentDepartmentId == dept?.Id
                && ((d.Status == DocumentStatus.PqlnPendingReceive)
                    || (d.PqlnReceiverId == user.Id && d.Status is DocumentStatus.PqlnReceived or DocumentStatus.ReturnedToPqln));
        return false;
    }

    public static DebtDocumentDto Map(
        DebtDocument d,
        Guid? pkhDeptId = null,
        Guid? pqlnDeptId = null,
        Guid? pdvDeptId = null,
        AppUser? viewer = null,
        string? viewerDeptCode = null)
    {
        var originDept = GetOriginDepartment(d);
        var originCode = GetOriginDepartmentCode(d);
        var originName = originDept?.Name ?? WorkflowConstants.GetDepartmentDisplayName(originCode);

        var visibleItems = viewer == null
            ? d.DebtItems.ToList()
            : d.DebtItems.Where(x => CanViewDebtItem(viewer, viewerDeptCode, x, d.WorkflowType, pkhDeptId, pqlnDeptId, pdvDeptId, originCode)).ToList();

        return new(
            d.Id, d.DocumentNumber, d.CustomerName, d.CustomerCif,
            NormalizeCustomerType(d.CustomerType),
            NormalizeWorkflowType(d.WorkflowType),
            d.IssueDate.ToString("yyyy-MM-dd"), d.DueDate.ToString("yyyy-MM-dd"),
            d.CurrentDepartmentId, d.CurrentDepartment?.Name, d.CurrentDepartment?.Code,
            d.Status.ToString(), d.Priority.ToString().ToLower(),
            d.Notes, d.HasDebtDocuments,
            d.CreatedByUserId, d.CreatedByUser?.Name,
            d.PqlnReceiverId, d.PqlnReceiver?.Name, d.PqlnReceivedAt,
            d.PdvReceiverId, d.PdvReceiver?.Name, d.PdvReceivedAt,
            d.EntryPostedAt, d.CompletedAt,
            visibleItems.Count,
            visibleItems.Count(x => IsItemCompleted(x, d.WorkflowType, pkhDeptId, pqlnDeptId, pdvDeptId)),
            d.CreatedAt, d.UpdatedAt,
            d.ReturnReason, d.ReturnedAt, d.ReturnedByUserId, d.ReturnedByUserName, d.ReturnedFromDepartmentCode,
            originCode, originName);
    }

    public static DebtItemDto Item(
        DebtItem x,
        string? pqlnVerifierName = null,
        string? pdvVerifierName = null,
        string? fallbackTargetCode = null,
        string? fallbackTargetName = null) => new(
        x.Id, x.DebtDocumentId, x.ItemName, x.IsSupplemented,
        x.SupplementedAt, x.SupplementedByUserId, x.SupplementedByUser?.Name, x.Notes,
        x.SourceDepartmentId, x.SourceDepartment?.Name, x.SourceDepartment?.Code,
        x.CreatedByUserId, x.CreatedByUser?.Name, x.CreatedAt,
        x.IsVerifiedByPqln, x.PqlnVerifiedAt, x.PqlnVerifiedByUserId, pqlnVerifierName,
        x.IsVerifiedByPdv, x.PdvVerifiedAt, x.PdvVerifiedByUserId, pdvVerifierName,
        x.LastRejectReason, x.RejectedAt, x.RejectedByUserId, x.RejectedByUserName, x.RejectedByDepartmentCode,
        x.TargetDepartmentId, x.TargetDepartment?.Name ?? fallbackTargetName, x.TargetDepartment?.Code ?? fallbackTargetCode);

    public static CirculationHistoryDto Hist(CirculationHistory x, string? overrideNotes = null) => new(
        x.Id, x.FromDepartmentId, x.FromDepartment?.Name,
        x.ToDepartmentId, x.ToDepartment?.Name,
        x.Action.ToString(), x.StatusAfter?.ToString(),
        x.PerformedByUserId, x.PerformedByUser?.Name,
        overrideNotes ?? x.Notes, x.CreatedAt);

    public static EditLogDto Log(DocumentEditLog x) => new(
        x.Id, x.EditedByUserId, x.EditedByUser?.Name, x.ChangesJson, x.CreatedAt);
}