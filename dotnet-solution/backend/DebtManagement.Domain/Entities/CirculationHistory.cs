namespace DebtManagement.Domain.Entities;

public enum CirculationAction
{
    Created,             // NV PKH khởi tạo
    PkhSupApproved,      // SUP PKH duyệt
    PqlnReceived,        // NV PQLN tiếp nhận
    PqlnSubmittedSup,    // NV PQLN đẩy SUP PQLN
    PqlnSupApproved,     // SUP PQLN duyệt & chuyển PDV
    PdvReceived,         // NV PDV tiếp nhận
    PdvEntryPosted,      // NV PDV đi bút toán
    PdvSupApprovedEntry, // SUP PDV duyệt lần 1 (Bút toán)
    Supplemented,        // Bổ sung chứng từ nợ
    Completed,           // ĐÃ HOÀN TẤT
    Rejected,            // Từ chối
    Returned             // Trả về
}

public class CirculationHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DebtDocumentId { get; set; }
    public Guid? FromDepartmentId { get; set; }
    public Guid? ToDepartmentId { get; set; }
    public CirculationAction Action { get; set; }
    public DocumentStatus? StatusAfter { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DebtDocument? DebtDocument { get; set; }
    public Department? FromDepartment { get; set; }
    public Department? ToDepartment { get; set; }
    public AppUser? PerformedByUser { get; set; }
}



