using DebtManagement.Shared.DTOs;
using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;

namespace DebtManagement.Application.Services;

internal static class DebtDocumentRules
{
    public const string WorkflowStandard = "pkh_pqln_pdv";
    public const string WorkflowDirectPdv = "pkh_pdv";

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

    public static bool IsItemCompleted(DebtItem x, string workflowType, Guid? pkhDeptId = null, Guid? pqlnDeptId = null)
    {
        if (!x.IsSupplemented) return false;

        // Luồng chuyển thẳng PKH -> PDV: chứng từ PKH được xác nhận trực tiếp bởi PDV
        if (NormalizeWorkflowType(workflowType) == WorkflowDirectPdv)
            return x.IsVerifiedByPdv;

        // Luồng tiêu chuẩn PKH -> PQLN -> PDV
        var isPkhItem = x.SourceDepartment?.Code == "PKH"
            || x.SourceDepartmentId == null
            || (pkhDeptId.HasValue && x.SourceDepartmentId == pkhDeptId.Value);
        if (isPkhItem) return x.IsVerifiedByPqln;

        var isPqlnItem = x.SourceDepartment?.Code == "PQLN"
            || (pqlnDeptId.HasValue && x.SourceDepartmentId == pqlnDeptId.Value);
        if (isPqlnItem) return x.IsVerifiedByPdv;

        return true;
    }

    public static int SupplementedCount(DebtDocument d, Guid? pkhDeptId = null, Guid? pqlnDeptId = null)
        => d.DebtItems.Count(x => IsItemCompleted(x, d.WorkflowType, pkhDeptId, pqlnDeptId));

    public static string NormalizeCustomerType(string? raw)
        => string.Equals(raw?.Trim(), "individual", StringComparison.OrdinalIgnoreCase)
            ? "individual"
            : "company";

    public static string NormalizeWorkflowType(string? raw)
        => string.Equals(raw?.Trim(), WorkflowDirectPdv, StringComparison.OrdinalIgnoreCase)
            ? WorkflowDirectPdv
            : WorkflowStandard;

    /// <summary>Quyền xem hồ sơ: admin/SUP PKH xem toàn bộ; SUP PQLN xem hồ sơ từ khi chuyển sang PQLN; SUP PDV xem hồ sơ từ khi chuyển sang PDV; nhân viên chỉ xem hồ sơ của mình.</summary>
    public static bool CanView(AppUser user, Department? dept, DebtDocument d)
    {
        if (user.Role == "admin") return true;

        var code = dept?.Code;
        var wf = NormalizeWorkflowType(d.WorkflowType);

        // Phòng PQLN không xem được các hồ sơ thuộc luồng chuyển thẳng PKH -> PDV
        if (code == "PQLN" && wf == WorkflowDirectPdv)
            return false;

        if (user.Role == "manager")
        {
            if (code == "PQLN")
            {
                return d.CurrentDepartmentId == dept?.Id
                    || d.Status is DocumentStatus.PqlnPendingReceive
                        or DocumentStatus.PqlnReceived
                        or DocumentStatus.PqlnPendingSup
                        or DocumentStatus.PdvPendingReceive
                        or DocumentStatus.PdvReceived
                        or DocumentStatus.PdvEntryPosted
                        or DocumentStatus.PdvEntryApproved
                        or DocumentStatus.Completed;
            }

            if (code == "PDV")
            {
                return d.CurrentDepartmentId == dept?.Id
                    || d.Status is DocumentStatus.PdvPendingReceive
                        or DocumentStatus.PdvReceived
                        or DocumentStatus.PdvEntryPosted
                        or DocumentStatus.PdvEntryApproved
                        or DocumentStatus.Completed;
            }

            return true;
        }

        if (code == "PKH") return d.CreatedByUserId == user.Id;
        if (code == "PQLN") return (d.CurrentDepartmentId == dept?.Id && d.Status == DocumentStatus.PqlnPendingReceive) || d.PqlnReceiverId == user.Id;
        if (code == "PDV") return (d.CurrentDepartmentId == dept?.Id && d.Status == DocumentStatus.PdvPendingReceive) || d.PdvReceiverId == user.Id;
        return false;
    }

    /// <summary>Quyền sửa thông tin hồ sơ: NV PKH sửa hồ sơ mình tạo; NV PQLN sửa hồ sơ mình tiếp nhận.</summary>
    public static bool CanEdit(AppUser user, Department? dept, DebtDocument d)
    {
        if (user.Role is "admin" or "manager") return false;
        var code = dept?.Code;
        if (code == "PKH") return d.CreatedByUserId == user.Id && d.CurrentDepartmentId == dept!.Id;
        if (code == "PQLN" && NormalizeWorkflowType(d.WorkflowType) == WorkflowStandard)
            return (d.CurrentDepartmentId == dept?.Id && d.Status == DocumentStatus.PqlnPendingReceive) || d.PqlnReceiverId == user.Id;
        return false;
    }

    public static DebtDocumentDto Map(DebtDocument d, Guid? pkhDeptId = null, Guid? pqlnDeptId = null) => new(
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
        d.DebtItems.Count, SupplementedCount(d, pkhDeptId, pqlnDeptId),
        d.CreatedAt, d.UpdatedAt);

    public static DebtItemDto Item(DebtItem x, string? pqlnVerifierName = null, string? pdvVerifierName = null) => new(
        x.Id, x.DebtDocumentId, x.ItemName, x.IsSupplemented,
        x.SupplementedAt, x.SupplementedByUserId, x.SupplementedByUser?.Name, x.Notes,
        x.SourceDepartmentId, x.SourceDepartment?.Name, x.SourceDepartment?.Code,
        x.CreatedByUserId, x.CreatedByUser?.Name, x.CreatedAt,
        x.IsVerifiedByPqln, x.PqlnVerifiedAt, x.PqlnVerifiedByUserId, pqlnVerifierName,
        x.IsVerifiedByPdv, x.PdvVerifiedAt, x.PdvVerifiedByUserId, pdvVerifierName);

    public static CirculationHistoryDto Hist(CirculationHistory x) => new(
        x.Id, x.FromDepartmentId, x.FromDepartment?.Name,
        x.ToDepartmentId, x.ToDepartment?.Name,
        x.Action.ToString(), x.StatusAfter?.ToString(),
        x.PerformedByUserId, x.PerformedByUser?.Name,
        x.Notes, x.CreatedAt);

    public static EditLogDto Log(DocumentEditLog x) => new(
        x.Id, x.EditedByUserId, x.EditedByUser?.Name, x.ChangesJson, x.CreatedAt);
}