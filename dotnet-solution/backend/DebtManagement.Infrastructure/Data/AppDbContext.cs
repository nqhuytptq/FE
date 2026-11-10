using DebtManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DebtManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DebtDocument> DebtDocuments => Set<DebtDocument>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<CirculationHistory> CirculationHistory => Set<CirculationHistory>();
    public DbSet<DocumentEditLog> DocumentEditLogs => Set<DocumentEditLog>();
    public DbSet<DebtItem> DebtItems => Set<DebtItem>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.Name).HasColumnName("name").HasMaxLength(100);
            e.Property(d => d.Code).HasColumnName("code").HasMaxLength(10);
            e.Property(d => d.Description).HasColumnName("description").HasMaxLength(500);
            e.Property(d => d.SortOrder).HasColumnName("sort_order");
            e.Property(d => d.CreatedAt).HasColumnName("created_at");
            e.HasIndex(d => d.Code).IsUnique();
        });

        mb.Entity<AppUser>(e =>
        {
            e.ToTable("app_users");
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Name).HasColumnName("name").HasMaxLength(100);
            e.Property(u => u.Email).HasColumnName("email").HasMaxLength(255);
            e.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            e.Property(u => u.Role).HasColumnName("role").HasMaxLength(20);
            e.Property(u => u.DepartmentId).HasColumnName("department_id");
            e.Property(u => u.IsActive).HasColumnName("is_active");
            e.Property(u => u.CreatedAt).HasColumnName("created_at");
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Department)
             .WithMany(d => d.Users)
             .HasForeignKey(u => u.DepartmentId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<DebtDocument>(e =>
        {
            e.ToTable("debt_documents");
            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.DocumentNumber).HasColumnName("document_number").HasMaxLength(50);
            e.Property(d => d.CustomerName).HasColumnName("customer_name").HasMaxLength(200);
            e.Property(d => d.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(20);
            e.Property(d => d.CustomerType).HasColumnName("customer_type")
             .HasConversion<string?>().HasMaxLength(20);
            e.Property(d => d.IssueDate).HasColumnName("issue_date");
            e.Property(d => d.DueDate).HasColumnName("due_date");
            e.Property(d => d.CurrentDepartmentId).HasColumnName("current_department_id");
            e.Property(d => d.Status).HasColumnName("status")
             .HasConversion<string>().HasMaxLength(30);
            e.Property(d => d.Priority).HasColumnName("priority")
             .HasConversion<string>().HasMaxLength(20);
            e.Property(d => d.Notes).HasColumnName("notes").HasMaxLength(2000);
            e.Property(d => d.HasDebtDocuments).HasColumnName("has_debt_documents");
            e.Property(d => d.CreatedByUserId).HasColumnName("created_by_user_id");
            e.Property(d => d.CreatedAt).HasColumnName("created_at");
            e.Property(d => d.UpdatedAt).HasColumnName("updated_at");
            e.Property(d => d.SupPkhApprovedAt).HasColumnName("sup_pkh_approved_at");
            e.Property(d => d.SupPkhApproverId).HasColumnName("sup_pkh_approver_id");
            e.Property(d => d.PqlnReceiverId).HasColumnName("pqln_receiver_id");
            e.Property(d => d.PqlnReceivedAt).HasColumnName("pqln_received_at");
            e.Property(d => d.SupPqlnApprovedAt).HasColumnName("sup_pqln_approved_at");
            e.Property(d => d.SupPqlnApproverId).HasColumnName("sup_pqln_approver_id");
            e.Property(d => d.PdvReceiverId).HasColumnName("pdv_receiver_id");
            e.Property(d => d.PdvReceivedAt).HasColumnName("pdv_received_at");
            e.Property(d => d.EntryPostedAt).HasColumnName("entry_posted_at");
            e.Property(d => d.EntryPostedById).HasColumnName("entry_posted_by_id");
            e.Property(d => d.SupPdvApprovedEntryAt).HasColumnName("sup_pdv_approved_entry_at");
            e.Property(d => d.SupPdvEntryApproverId).HasColumnName("sup_pdv_entry_approver_id");
            e.Property(d => d.CompletedAt).HasColumnName("completed_at");
            e.Property(d => d.CompletedByUserId).HasColumnName("completed_by_user_id");
            e.HasOne(d => d.CurrentDepartment).WithMany()
             .HasForeignKey(d => d.CurrentDepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.CreatedByUser).WithMany()
             .HasForeignKey(d => d.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.PqlnReceiver).WithMany()
             .HasForeignKey(d => d.PqlnReceiverId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.PdvReceiver).WithMany()
             .HasForeignKey(d => d.PdvReceiverId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(d => d.DocumentNumber).IsUnique();
            e.HasIndex(d => d.Status);
        });

        mb.Entity<DebtItem>(e =>
        {
            e.ToTable("debt_items");
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.DebtDocumentId).HasColumnName("debt_document_id");
            e.Property(i => i.ItemName).HasColumnName("item_name").HasMaxLength(255);
            e.Property(i => i.IsSupplemented).HasColumnName("is_supplemented");
            e.Property(i => i.SupplementedAt).HasColumnName("supplemented_at");
            e.Property(i => i.SupplementedByUserId).HasColumnName("supplemented_by_user_id");
            e.Property(i => i.Notes).HasColumnName("notes").HasMaxLength(1000);
            e.Property(i => i.SourceDepartmentId).HasColumnName("source_department_id");
            e.Property(i => i.CreatedByUserId).HasColumnName("created_by_user_id");
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.HasOne(i => i.DebtDocument).WithMany(d => d.DebtItems)
             .HasForeignKey(i => i.DebtDocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.SupplementedByUser).WithMany()
             .HasForeignKey(i => i.SupplementedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.SourceDepartment).WithMany()
             .HasForeignKey(i => i.SourceDepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.CreatedByUser).WithMany()
             .HasForeignKey(i => i.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(i => i.DebtDocumentId);
        });

        mb.Entity<CirculationHistory>(e =>
        {
            e.ToTable("circulation_history");
            e.Property(h => h.Id).HasColumnName("id");
            e.Property(h => h.DebtDocumentId).HasColumnName("debt_document_id");
            e.Property(h => h.FromDepartmentId).HasColumnName("from_department_id");
            e.Property(h => h.ToDepartmentId).HasColumnName("to_department_id");
            e.Property(h => h.Action).HasColumnName("action")
             .HasConversion<string>().HasMaxLength(30);
            e.Property(h => h.StatusAfter).HasColumnName("status_after")
             .HasConversion<string?>().HasMaxLength(30);
            e.Property(h => h.PerformedByUserId).HasColumnName("performed_by_user_id");
            e.Property(h => h.Notes).HasColumnName("notes").HasMaxLength(2000);
            e.Property(h => h.CreatedAt).HasColumnName("created_at");
            e.HasOne(h => h.DebtDocument).WithMany(d => d.History)
             .HasForeignKey(h => h.DebtDocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(h => h.FromDepartment).WithMany()
             .HasForeignKey(h => h.FromDepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(h => h.ToDepartment).WithMany()
             .HasForeignKey(h => h.ToDepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(h => h.PerformedByUser).WithMany()
             .HasForeignKey(h => h.PerformedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(h => h.DebtDocumentId);
        });

        mb.Entity<DocumentEditLog>(e =>
        {
            e.ToTable("document_edit_logs");
            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.DebtDocumentId).HasColumnName("debt_document_id");
            e.Property(l => l.EditedByUserId).HasColumnName("edited_by_user_id");
            e.Property(l => l.ChangesJson).HasColumnName("changes").HasColumnType("json");
            e.Property(l => l.CreatedAt).HasColumnName("created_at");
            e.HasOne(l => l.DebtDocument).WithMany(d => d.EditLogs)
             .HasForeignKey(l => l.DebtDocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.EditedByUser).WithMany()
             .HasForeignKey(l => l.EditedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(l => l.DebtDocumentId);
        });

        ConfigureSeedData(mb);
    }

    private static void ConfigureSeedData(ModelBuilder mb)
    {
        var pkhId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var pqlnId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var pdvId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        mb.Entity<Department>().HasData(
            new Department { Id = pkhId, Name = "Phòng Khách hàng", Code = "PKH", Description = "Tiếp nhận và khởi tạo hồ sơ văn bản nợ", SortOrder = 1, CreatedAt = epoch },
            new Department { Id = pqlnId, Name = "Phòng Quản lý nợ", Code = "PQLN", Description = "Thẩm định, tiếp nhận và kiểm soát chứng từ nợ", SortOrder = 2, CreatedAt = epoch },
            new Department { Id = pdvId, Name = "Phòng Dịch vụ khách hàng", Code = "PDV", Description = "Tiếp nhận, đi bút toán và hoàn tất hồ sơ", SortOrder = 3, CreatedAt = epoch }
        );

        var staffHash = BCrypt.Net.BCrypt.HashPassword("Staff@123456");
        var supHash = BCrypt.Net.BCrypt.HashPassword("Sup@123456");
        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456");

        mb.Entity<AppUser>().HasData(
            new AppUser { Id = Guid.Parse("00000000-0000-0000-0000-000000000010"), Name = "Quản trị viên", Email = "admin@vcb.com", PasswordHash = adminHash, Role = "admin", DepartmentId = pkhId, IsActive = true, CreatedAt = epoch },
            new AppUser { Id = Guid.Parse("00000000-0000-0000-0000-000000000011"), Name = "Nguyễn Văn Khách Hàng (NV)", Email = "nv.pkh@vcb.com", PasswordHash = staffHash, Role = "staff", DepartmentId = pkhId, IsActive = true, CreatedAt = epoch },
            new AppUser { Id = Guid.Parse("00000000-0000-0000-0000-000000000012"), Name = "Trần Trưởng Phòng KH (SUP)", Email = "sup.pkh@vcb.com", PasswordHash = supHash, Role = "manager", DepartmentId = pkhId, IsActive = true, CreatedAt = epoch },
            new AppUser { Id = Guid.Parse("00000000-0000-0000-0000-000000000021"), Name = "Lê Quản Lý Nợ (NV)", Email = "nv.pqln@vcb.com", PasswordHash = staffHash, Role = "staff", DepartmentId = pqlnId, IsActive = true, CreatedAt = epoch },
            new AppUser { Id = Guid.Parse("00000000-0000-0000-0000-000000000022"), Name = "Phạm Trưởng Phòng QLN (SUP)", Email = "sup.pqln@vcb.com", PasswordHash = supHash, Role = "manager", DepartmentId = pqlnId, IsActive = true, CreatedAt = epoch },
            new AppUser { Id = Guid.Parse("00000000-0000-0000-0000-000000000031"), Name = "Hoàng Dịch Vụ KH (NV)", Email = "nv.pdv@vcb.com", PasswordHash = staffHash, Role = "staff", DepartmentId = pdvId, IsActive = true, CreatedAt = epoch },
            new AppUser { Id = Guid.Parse("00000000-0000-0000-0000-000000000032"), Name = "Vũ Trưởng Phòng DVKH (SUP)", Email = "sup.pdv@vcb.com", PasswordHash = supHash, Role = "manager", DepartmentId = pdvId, IsActive = true, CreatedAt = epoch }
        );
    }
}
