using DebtManagement.Shared.DTOs;
using static DebtManagement.Shared.Constants.WorkflowConstants;

namespace DebtManagement.BlazorApp.Models;

public record DeptOption(Guid Id, string Name, string Code);

public static class DebtUiHelpers
{
    public static readonly int[] PageSizeOptions = [5, 10, 20, 50, 100];

    public static List<int?> GetVisiblePages(int current, int total)
    {
        if (total <= 7)
            return Enumerable.Range(1, total).Select(x => (int?)x).ToList();

        var pages = new SortedSet<int> { 1, total, current };
        if (current > 1) pages.Add(current - 1);
        if (current < total) pages.Add(current + 1);

        var result = new List<int?>();
        int prev = 0;
        foreach (var p in pages)
        {
            if (prev > 0 && p - prev > 1) result.Add(null);
            result.Add(p);
            prev = p;
        }
        return result;
    }

    public static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "VC";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpper()
            : $"{parts[^2][0]}{parts[^1][0]}".ToUpper();
    }

    public static string RoleLabel(string? role) => role switch
    {
        RoleStaff => "Nhân viên",
        RoleManager => "LĐP (SUP)",
        RoleAdmin => "Quản trị viên",
        _ => role ?? ""
    };

    public static DateTime ToVnTime(DateTime dt) =>
        dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime().AddHours(7) : dt.AddHours(7);

    public static string FormatVnTime(DateTime dt) => ToVnTime(dt).ToString("dd/MM HH:mm");
    public static string FormatFullVnTime(DateTime dt) => ToVnTime(dt).ToString("dd/MM/yyyy HH:mm:ss");
    public static string FormatDate(string yyyyMmDd) => DateOnly.TryParse(yyyyMmDd, out var date) ? date.ToString("dd/MM/yyyy") : yyyyMmDd;

    public static bool IsOverdue(DebtDocumentDto d)
        => d.Status != StatusNames.Completed
        && DateOnly.TryParse(d.DueDate, out var due)
        && due < DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

    public static string StatusPillClass(string s) => s switch
    {
        StatusNames.Completed => "pill-green",
        StatusNames.PqlnReceived or StatusNames.PdvReceived or StatusNames.PdvEntryPosted => "pill-blue",
        StatusNames.PdvEntryApproved or StatusNames.PkhPendingSup or StatusNames.PqlnPendingReceive or StatusNames.PqlnPendingSup or StatusNames.PdvPendingReceive => "pill-amber",
        _ => "pill-gray"
    };

    public static string StatusLabel(string s) => s switch
    {
        StatusNames.PkhPendingSup => "Chờ SUP PKH",
        StatusNames.PqlnPendingReceive => "Chờ PQLN nhận",
        StatusNames.PqlnReceived => "PQLN tiếp nhận",
        StatusNames.PqlnPendingSup => "Chờ SUP PQLN",
        StatusNames.PdvPendingReceive => "Chờ PDV nhận",
        StatusNames.PdvReceived => "PDV tiếp nhận",
        StatusNames.PdvEntryPosted => "Chờ SUP PDV duyệt",
        StatusNames.PdvEntryApproved => "Chờ bổ sung CT",
        StatusNames.Completed => "Hoàn tất",
        _ => s
    };

    public static string CurrentHandlerText(DebtDocumentDto d) => d.Status switch
    {
        StatusNames.PkhPendingSup => $"Chờ SUP PKH ({d.CreatedByUserName})",
        StatusNames.PqlnPendingReceive => "PQLN: Chờ tiếp nhận",
        StatusNames.PqlnReceived or StatusNames.PqlnPendingSup => $"PQLN: {d.PqlnReceiverName ?? "—"}",
        StatusNames.PdvPendingReceive => "PDV: Chờ tiếp nhận",
        StatusNames.PdvReceived or StatusNames.PdvEntryPosted or StatusNames.PdvEntryApproved => $"PDV: {d.PdvReceiverName ?? "—"}",
        StatusNames.Completed => $"Hoàn tất ({d.PdvReceiverName ?? d.CreatedByUserName})",
        _ => d.CreatedByUserName ?? "—"
    };

    public static bool IsItemFullyCompleted(DebtItemDto x, bool isDirectPdv)
    {
        if (!x.IsSupplemented) return false;
        if (isDirectPdv) return x.IsVerifiedByPdv;

        var deptCode = x.SourceDepartmentCode ?? DeptPkh;
        return deptCode switch
        {
            DeptPkh => x.IsVerifiedByPqln,
            DeptPqln => x.IsVerifiedByPdv,
            _ => true
        };
    }

    public static bool IsActionRequiredForUser(DebtDocumentDto d, Guid? userId, string? deptCode, string? role)
        => GetMyActionLabel(d, userId, deptCode, role) != null;

    public static string? GetMyActionLabel(DebtDocumentDto d, Guid? userId, string? deptCode, string? role)
    {
        if (!userId.HasValue || d.Status == StatusNames.Completed) return null;

        if (role == RoleManager)
        {
            if (deptCode == DeptPkh && d.Status == StatusNames.PkhPendingSup) return "Duyệt PKH";
            if (deptCode == DeptPqln && d.WorkflowType != WorkflowDirectPdv && d.Status == StatusNames.PqlnPendingSup) return "Duyệt PQLN";
            if (deptCode == DeptPdv && d.Status == StatusNames.PdvEntryPosted)
                return d.TotalDebtItems > d.SupplementedDebtItems ? "Duyệt bút toán" : "Duyệt hoàn tất";
            if (deptCode == DeptPdv && d.Status == StatusNames.PdvEntryApproved && d.TotalDebtItems <= d.SupplementedDebtItems)
                return "Duyệt hoàn tất";
            return null;
        }

        if (role == RoleStaff)
        {
            if (deptCode == DeptPkh && d.CreatedByUserId == userId.Value && d.TotalDebtItems > d.SupplementedDebtItems)
                return "Bổ sung CT";

            if (deptCode == DeptPqln && d.WorkflowType != WorkflowDirectPdv)
            {
                if (d.Status == StatusNames.PqlnPendingReceive) return "Tiếp nhận";
                if (d.PqlnReceiverId == userId.Value)
                {
                    if (d.Status == StatusNames.PqlnReceived) return "Đẩy SUP PQLN";
                    if (d.TotalDebtItems > d.SupplementedDebtItems) return "Bổ sung / XN CT";
                }
            }

            if (deptCode == DeptPdv)
            {
                if (d.Status == StatusNames.PdvPendingReceive) return "Tiếp nhận";
                if (d.PdvReceiverId == userId.Value)
                {
                    if (d.Status == StatusNames.PdvReceived) return "Đi bút toán";
                    if (d.TotalDebtItems > d.SupplementedDebtItems) return "Xác nhận CT";
                }
            }
        }

        return null;
    }
}