namespace DebtManagement.Shared.Constants;

public static class WorkflowConstants
{
    // Luồng xử lý hồ sơ
    public const string WorkflowStandard = "pkh_pqln_pdv";
    public const string WorkflowDirectPdv = "pkh_pdv";

    // Đối tượng khách hàng
    public const string CustomerCompany = "company";
    public const string CustomerIndividual = "individual";

    // Mã phòng ban (nhóm luồng & phòng nghiệp vụ)
    public const string DeptPkh = "PKH";
    public const string DeptPqln = "PQLN";
    public const string DeptPdv = "PDV";
    public const string DeptPkt = "PKT";

    // Mã 4 phòng bán hàng thực tế
    public const string DeptKhdn = "KHDN";
    public const string DeptKhbl = "KHBL";
    public const string DeptPgdLlq = "PGD_LLQ";
    public const string DeptPgdCd = "PGD_CD";

    public static readonly HashSet<string> SalesDepartmentCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        DeptKhdn,
        DeptKhbl,
        DeptPgdLlq,
        DeptPgdCd,
        DeptPkh // Giữ lại PKH để tương thích dữ liệu lịch sử
    };

    public static bool IsSalesDept(string? deptCode)
        => !string.IsNullOrWhiteSpace(deptCode) && SalesDepartmentCodes.Contains(deptCode.Trim());

    public static string GetWorkflowGroup(string? deptCode)
        => IsSalesDept(deptCode) ? DeptPkh : (deptCode?.Trim().ToUpperInvariant() ?? "");

    // Vai trò người dùng
    public const string RoleAdmin = "admin";
    public const string RoleManager = "manager";
    public const string RoleStaff = "staff";

    // Tên trạng thái hồ sơ (khớp enum DocumentStatus)
    public static class StatusNames
    {
        public const string PkhPendingSup = "PkhPendingSup";
        public const string PqlnPendingReceive = "PqlnPendingReceive";
        public const string PqlnReceived = "PqlnReceived";
        public const string PqlnPendingSup = "PqlnPendingSup";
        public const string PdvPendingReceive = "PdvPendingReceive";
        public const string PdvReceived = "PdvReceived";
        public const string PdvEntryPosted = "PdvEntryPosted";
        public const string PdvEntryApproved = "PdvEntryApproved";
        public const string Completed = "Completed";
        public const string ReturnedToPkh = "ReturnedToPkh";
        public const string ReturnedToPqln = "ReturnedToPqln";
    }
}