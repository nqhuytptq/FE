namespace DebtManagement.Shared.DTOs;

public record DepartmentDto(Guid Id, string Name, string Code, string? Description, int SortOrder);
public record AppUserDto(Guid Id, string Name, string Email, string Role, Guid? DepartmentId, string? DepartmentName, string? DepartmentCode, bool IsActive, DateTime CreatedAt);
public record CreateUserRequest(string Name, string Email, string Password, Guid DepartmentId, string Role);
public record UpdateUserRequest(string Name, Guid DepartmentId, string Role, bool IsActive, string? Password);
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, AppUserDto User);

public record DebtItemDto(
    Guid Id,
    Guid DebtDocumentId,
    string ItemName,
    bool IsSupplemented,
    DateTime? SupplementedAt,
    Guid? SupplementedByUserId,
    string? SupplementedByUserName,
    string? Notes,
    Guid? SourceDepartmentId,
    string? SourceDepartmentName,
    string? SourceDepartmentCode,
    Guid? CreatedByUserId,
    string? CreatedByUserName,
    DateTime CreatedAt,
    bool IsVerifiedByPqln = false,
    DateTime? PqlnVerifiedAt = null,
    Guid? PqlnVerifiedByUserId = null,
    string? PqlnVerifiedByUserName = null,
    bool IsVerifiedByPdv = false,
    DateTime? PdvVerifiedAt = null,
    Guid? PdvVerifiedByUserId = null,
    string? PdvVerifiedByUserName = null,
    string? LastRejectReason = null,
    DateTime? RejectedAt = null,
    Guid? RejectedByUserId = null,
    string? RejectedByUserName = null,
    string? RejectedByDepartmentCode = null,
    Guid? TargetDepartmentId = null,
    string? TargetDepartmentName = null,
    string? TargetDepartmentCode = null);

public record SupplementItemsRequest(List<Guid> SupplementedItemIds, string? Notes);
public record AddDebtItemsRequest(List<CreateDebtItemRequest> Items, string? Notes);
public record UpdateDebtItemRequest(string ItemName, string? Notes, string? TargetDepartmentCode = null);

public record DebtDocumentDto(
    Guid Id,
    string DocumentNumber,
    string CustomerName,
    string? CustomerCif,
    string CustomerType,
    string WorkflowType,
    string IssueDate,
    string DueDate,
    Guid? CurrentDepartmentId,
    string? CurrentDepartmentName,
    string? CurrentDepartmentCode,
    string Status,
    string Priority,
    string? Notes,
    bool HasDebtDocuments,
    Guid? CreatedByUserId,
    string? CreatedByUserName,
    Guid? PqlnReceiverId,
    string? PqlnReceiverName,
    DateTime? PqlnReceivedAt,
    Guid? PdvReceiverId,
    string? PdvReceiverName,
    DateTime? PdvReceivedAt,
    DateTime? EntryPostedAt,
    DateTime? CompletedAt,
    int TotalDebtItems,
    int SupplementedDebtItems,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? ReturnReason = null,
    DateTime? ReturnedAt = null,
    Guid? ReturnedByUserId = null,
    string? ReturnedByUserName = null,
    string? ReturnedFromDepartmentCode = null);

public record DebtDocumentDetailDto(
    Guid Id,
    string DocumentNumber,
    string CustomerName,
    string? CustomerCif,
    string CustomerType,
    string WorkflowType,
    string IssueDate,
    string DueDate,
    Guid? CurrentDepartmentId,
    string? CurrentDepartmentName,
    string? CurrentDepartmentCode,
    string Status,
    string Priority,
    string? Notes,
    bool HasDebtDocuments,
    Guid? CreatedByUserId,
    string? CreatedByUserName,
    DateTime CreatedAt,
    DateTime? SupPkhApprovedAt,
    Guid? SupPkhApproverId,
    string? SupPkhApproverName,
    Guid? PqlnReceiverId,
    string? PqlnReceiverName,
    DateTime? PqlnReceivedAt,
    DateTime? SupPqlnApprovedAt,
    Guid? SupPqlnApproverId,
    string? SupPqlnApproverName,
    Guid? PdvReceiverId,
    string? PdvReceiverName,
    DateTime? PdvReceivedAt,
    DateTime? EntryPostedAt,
    Guid? EntryPostedById,
    string? EntryPostedByName,
    DateTime? SupPdvApprovedEntryAt,
    Guid? SupPdvEntryApproverId,
    string? SupPdvEntryApproverName,
    DateTime? CompletedAt,
    Guid? CompletedByUserId,
    string? CompletedByUserName,
    DateTime UpdatedAt,
    IReadOnlyList<DebtItemDto> DebtItems,
    IReadOnlyList<CirculationHistoryDto> CirculationHistory,
    IReadOnlyList<EditLogDto> EditLogs,
    string? ReturnReason = null,
    DateTime? ReturnedAt = null,
    Guid? ReturnedByUserId = null,
    string? ReturnedByUserName = null,
    string? ReturnedFromDepartmentCode = null,
    int HiddenDebtItemsCount = 0);

public record CirculationHistoryDto(
    Guid Id, Guid? FromDepartmentId, string? FromDepartmentName,
    Guid? ToDepartmentId, string? ToDepartmentName,
    string Action, string? StatusAfter,
    Guid? PerformedByUserId, string? PerformedByUserName,
    string? Notes, DateTime CreatedAt);

public record EditLogDto(Guid Id, Guid? EditedByUserId, string? EditedByUserName, string ChangesJson, DateTime CreatedAt);

public class CreateDebtItemRequest
{
    public string ItemName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? TargetDepartmentCode { get; set; } // "PQLN" hoặc "PDV"
}

public class CreateDebtDocumentRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerCif { get; set; }
    public string CustomerType { get; set; } = "company";
    public string WorkflowType { get; set; } = "pkh_pqln_pdv";
    public string IssueDate { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public string? Notes { get; set; }
    public bool HasDebtDocuments { get; set; }
    public List<CreateDebtItemRequest>? DebtItems { get; set; }
}

public class UpdateDebtDocumentRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerCif { get; set; }
    public string CustomerType { get; set; } = "company";
    public string WorkflowType { get; set; } = "pkh_pqln_pdv";
    public string IssueDate { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public string? Notes { get; set; }
    public bool HasDebtDocuments { get; set; }
    public List<CreateDebtItemRequest>? DebtItems { get; set; }
}

public record ActionWithNotesRequest(string? Notes);
public record VerifySupplementItemsRequest(List<Guid> ItemIds, string? Notes = null);
public record VerifyPqlnSupplementItemsRequest(List<Guid> ItemIds, string? Notes = null);
public record RejectSupplementItemRequest(string Reason);