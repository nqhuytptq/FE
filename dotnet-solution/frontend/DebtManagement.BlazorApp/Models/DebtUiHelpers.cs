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
        StatusNames.ReturnedToPkh or StatusNames.ReturnedToPqln => "pill-red",
        _ => "pill-gray"
    };

    public static string StatusLabel(string s) => s switch
    {
        StatusNames.PkhPendingSup => "Chờ SUP duyệt",
        StatusNames.PqlnPendingReceive => "Chờ PQLN nhận",
        StatusNames.PqlnReceived => "PQLN tiếp nhận",
        StatusNames.PqlnPendingSup => "Chờ SUP PQLN",
        StatusNames.PdvPendingReceive => "Chờ PDV nhận",
        StatusNames.PdvReceived => "PDV tiếp nhận",
        StatusNames.PdvEntryPosted => "Chờ SUP PDV duyệt",
        StatusNames.PdvEntryApproved => "Đã duyệt BT · Chờ CT",
        StatusNames.ReturnedToPkh => "Trả về phòng tạo",
        StatusNames.ReturnedToPqln => "Trả về PQLN",
        StatusNames.Completed => "Hoàn tất",
        _ => s
    };

    public static string CurrentHandlerText(DebtDocumentDto d)
    {
        var origin = d.OriginDepartmentCode ?? DeptPkh;
        return d.Status switch
        {
            StatusNames.PkhPendingSup => $"Chờ SUP {origin} ({d.CreatedByUserName})",
            StatusNames.ReturnedToPkh => $"{origin} sửa lại ({d.CreatedByUserName})",
            StatusNames.PqlnPendingReceive => "PQLN: Chờ tiếp nhận",
            StatusNames.PqlnReceived or StatusNames.PqlnPendingSup => $"PQLN: {d.PqlnReceiverName ?? "—"}",
            StatusNames.ReturnedToPqln => $"PQLN xử lý lại ({d.PqlnReceiverName ?? "—"})",
            StatusNames.PdvPendingReceive => "PDV: Chờ tiếp nhận",
            StatusNames.PdvReceived or StatusNames.PdvEntryPosted or StatusNames.PdvEntryApproved => $"PDV: {d.PdvReceiverName ?? "—"}",
            StatusNames.Completed => $"Hoàn tất ({d.PdvReceiverName ?? d.CreatedByUserName})",
            _ => d.CreatedByUserName ?? "—"
        };
    }

    public static string GetItemTargetDeptCode(DebtItemDto x, bool isDirectPdv)
    {
        if (!string.IsNullOrWhiteSpace(x.TargetDepartmentCode))
            return x.TargetDepartmentCode.Trim().ToUpperInvariant();
        if (isDirectPdv || string.Equals(x.SourceDepartmentCode, DeptPqln, StringComparison.OrdinalIgnoreCase))
            return DeptPdv;
        return DeptPqln;
    }

    public static bool IsItemFullyCompleted(DebtItemDto x, bool isDirectPdv)
    {
        if (!x.IsSupplemented) return false;
        return GetItemTargetDeptCode(x, isDirectPdv) == DeptPdv
            ? x.IsVerifiedByPdv
            : x.IsVerifiedByPqln;
    }

    public static bool IsActionRequiredForUser(DebtDocumentDto d, Guid? userId, string? deptCode, string? role)
        => GetMyActionLabel(d, userId, deptCode, role) != null;

    public static string? GetMyActionLabel(DebtDocumentDto d, Guid? userId, string? deptCode, string? role)
    {
        if (!userId.HasValue || d.Status == StatusNames.Completed) return null;

        if (role == RoleManager)
        {
            if (IsSalesDept(deptCode)
                && d.Status == StatusNames.PkhPendingSup
                && string.Equals(d.OriginDepartmentCode ?? DeptPkh, deptCode, StringComparison.OrdinalIgnoreCase))
            {
                return $"Duyệt {deptCode}";
            }
            if (deptCode == DeptPqln && d.WorkflowType != WorkflowDirectPdv && d.Status == StatusNames.PqlnPendingSup) return "Duyệt PQLN";
            if (deptCode == DeptPdv && d.Status == StatusNames.PdvEntryPosted)
                return d.TotalDebtItems > d.SupplementedDebtItems ? "Duyệt bút toán" : "Duyệt BT & hoàn tất";
            return null;
        }

        if (role == RoleStaff)
        {
            if (IsSalesDept(deptCode) && d.CreatedByUserId == userId.Value)
            {
                if (d.Status == StatusNames.ReturnedToPkh) return "Sửa & gửi lại";
                if (d.TotalDebtItems > d.SupplementedDebtItems) return "Bổ sung CT";
            }

            if (deptCode == DeptPqln && d.WorkflowType != WorkflowDirectPdv)
            {
                if (d.Status == StatusNames.PqlnPendingReceive) return "Tiếp nhận";
                if (d.PqlnReceiverId == userId.Value)
                {
                    if (d.Status == StatusNames.ReturnedToPqln) return "Xử lý trả lại";
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