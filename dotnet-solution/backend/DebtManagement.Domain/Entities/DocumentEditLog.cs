namespace DebtManagement.Domain.Entities;

public class DocumentEditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DebtDocumentId { get; set; }
    public Guid? EditedByUserId { get; set; }
    public string ChangesJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DebtDocument? DebtDocument { get; set; }
    public AppUser? EditedByUser { get; set; }
}

