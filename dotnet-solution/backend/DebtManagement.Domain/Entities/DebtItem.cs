namespace DebtManagement.Domain.Entities;

public class DebtItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DebtDocumentId { get; set; }
    public string ItemName { get; set; } = string.Empty;       // Tên loại chứng từ nợ
    public bool IsSupplemented { get; set; } = false;          // Đã bổ sung chưa
    public DateTime? SupplementedAt { get; set; }              // Thời gian bổ sung
    public Guid? SupplementedByUserId { get; set; }            // Người bổ sung
    public string? Notes { get; set; }                         // Ghi chú

    // Phòng ban khai báo chứng từ này (PKH / PQLN)
    public Guid? SourceDepartmentId { get; set; }
    // Người khai báo chứng từ
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DebtDocument? DebtDocument { get; set; }
    public AppUser? SupplementedByUser { get; set; }
    public Department? SourceDepartment { get; set; }
    public AppUser? CreatedByUser { get; set; }
}
