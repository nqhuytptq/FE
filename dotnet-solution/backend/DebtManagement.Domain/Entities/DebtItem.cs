namespace DebtManagement.Domain.Entities;

public class DebtItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DebtDocumentId { get; set; }
    public string ItemName { get; set; } = string.Empty;       // Tên loại chứng từ nợ
    public bool IsSupplemented { get; set; } = false;          // Đã bổ sung chưa
    public DateTime? SupplementedAt { get; set; }              // Thời gian bổ sung
    public Guid? SupplementedByUserId { get; set; }            // Người bổ sung

    public bool IsVerifiedByPqln { get; set; }
    public DateTime? PqlnVerifiedAt { get; set; }
    public Guid? PqlnVerifiedByUserId { get; set; }

    public bool IsVerifiedByPdv { get; set; }
    public DateTime? PdvVerifiedAt { get; set; }
    public Guid? PdvVerifiedByUserId { get; set; }
    public string? Notes { get; set; }                         // Ghi chú

    // Phòng ban nợ chứng từ này (PKH / PQLN)
    public Guid? SourceDepartmentId { get; set; }
    // Phòng ban được nợ / có trách nhiệm xác nhận chứng từ này (PQLN / PDV)
    public Guid? TargetDepartmentId { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Thông tin từ chối bổ sung ---
    public string? LastRejectReason { get; set; }
    public DateTime? RejectedAt { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public string? RejectedByUserName { get; set; }
    public string? RejectedByDepartmentCode { get; set; }

    public DebtDocument? DebtDocument { get; set; }
    public AppUser? SupplementedByUser { get; set; }
    public Department? SourceDepartment { get; set; }
    public Department? TargetDepartment { get; set; }
    public AppUser? CreatedByUser { get; set; }
}