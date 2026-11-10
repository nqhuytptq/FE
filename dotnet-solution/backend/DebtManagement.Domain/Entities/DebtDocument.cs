
namespace DebtManagement.Domain.Entities;

public enum DocumentStatus
{
    Draft,                  // Khởi tạo
    PkhPendingSup,          // Chờ SUP PKH duyệt
    PqlnPendingReceive,     // Chờ PQLN tiếp nhận
    PqlnReceived,           // PQLN đã tiếp nhận
    PqlnPendingSup,         // Chờ SUP PQLN duyệt
    PdvPendingReceive,      // Chờ PDV tiếp nhận
    PdvReceived,            // PDV đã tiếp nhận
    PdvEntryPosted,         // Đã đi bút toán (Chờ SUP PDV duyệt)
    PdvEntryApproved,       // SUP PDV đã duyệt bút toán (Chờ bổ sung đủ chứng từ nợ - Luồng 1)
    Completed,              // ĐÃ HOÀN TẤT
    Rejected                // Từ chối
}

public enum CustomerType
{
    Company,
    Individual
}

public enum DocumentPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public class DebtDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DocumentNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public CustomerType? CustomerType { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public Guid? CurrentDepartmentId { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.PkhPendingSup;
    public DocumentPriority Priority { get; set; } = DocumentPriority.Normal;
    public string? Notes { get; set; }

    // --- Chứng từ nợ ---
    public bool HasDebtDocuments { get; set; } = false; // Có chứng từ nợ không?

    // --- Audit & Tracking luồng ---
    public Guid? CreatedByUserId { get; set; }          // NV PKH khởi tạo
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SupPkhApprovedAt { get; set; }     // Thời gian duyệt SUP PKH
    public Guid? SupPkhApproverId { get; set; }

    public Guid? PqlnReceiverId { get; set; }           // Cán bộ PQLN tiếp nhận
    public DateTime? PqlnReceivedAt { get; set; }       // Thời gian tiếp nhận PQLN
    public DateTime? SupPqlnApprovedAt { get; set; }    // Thời gian duyệt SUP PQLN
    public Guid? SupPqlnApproverId { get; set; }

    public Guid? PdvReceiverId { get; set; }            // Cán bộ PDV tiếp nhận
    public DateTime? PdvReceivedAt { get; set; }        // Thời gian tiếp nhận PDV
    public DateTime? EntryPostedAt { get; set; }        // Thời gian đi bút toán
    public Guid? EntryPostedById { get; set; }

    public DateTime? SupPdvApprovedEntryAt { get; set; } // SUP PDV duyệt lần 1 (Bút toán)
    public Guid? SupPdvEntryApproverId { get; set; }

    public DateTime? CompletedAt { get; set; }          // Hoàn thành hồ sơ (Duyệt lần 2 hoặc duyệt lần 1 nếu đủ CT)
    public Guid? CompletedByUserId { get; set; }

    // --- Navigation ---
    public Department? CurrentDepartment { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public AppUser? PqlnReceiver { get; set; }
    public AppUser? PdvReceiver { get; set; }
    public ICollection<DebtItem> DebtItems { get; set; } = [];
    public ICollection<CirculationHistory> History { get; set; } = [];
    public ICollection<DocumentEditLog> EditLogs { get; set; } = [];
}



