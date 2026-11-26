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
    public DbSet<WorkCalendarHoliday> WorkCalendarHolidays => Set<WorkCalendarHoliday>();
    public DbSet<KpiConfig> KpiConfigs => Set<KpiConfig>();
    public DbSet<KpiPeriod> KpiPeriods => Set<KpiPeriod>();
    public DbSet<KpiViolationItem> KpiViolationItems => Set<KpiViolationItem>();
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
            e.Property(d => d.CustomerCif).HasColumnName("customer_cif").HasMaxLength(50);
            e.Property(d => d.CustomerType).HasColumnName("customer_type")
             .HasConversion<string?>().HasMaxLength(20);
            e.Property(d => d.WorkflowType).HasColumnName("workflow_type")
             .HasMaxLength(30).HasDefaultValue("pkh_pqln_pdv");
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
            e.Property(d => d.ReturnReason).HasColumnName("return_reason").HasMaxLength(2000);
            e.Property(d => d.ReturnedAt).HasColumnName("returned_at");
            e.Property(d => d.ReturnedByUserId).HasColumnName("returned_by_user_id");
            e.Property(d => d.ReturnedByUserName).HasColumnName("returned_by_user_name").HasMaxLength(100);
            e.Property(d => d.ReturnedFromDepartmentCode).HasColumnName("returned_from_department_code").HasMaxLength(20);
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
            e.Property(i => i.IsVerifiedByPqln).HasColumnName("is_verified_by_pqln");
            e.Property(i => i.PqlnVerifiedAt).HasColumnName("pqln_verified_at");
            e.Property(i => i.PqlnVerifiedByUserId).HasColumnName("pqln_verified_by_user_id");
            e.Property(i => i.IsVerifiedByPdv).HasColumnName("is_verified_by_pdv");
            e.Property(i => i.PdvVerifiedAt).HasColumnName("pdv_verified_at");
            e.Property(i => i.PdvVerifiedByUserId).HasColumnName("pdv_verified_by_user_id");
            e.Property(i => i.Notes).HasColumnName("notes").HasMaxLength(1000);
            e.Property(i => i.SourceDepartmentId).HasColumnName("source_department_id");
            e.Property(i => i.TargetDepartmentId).HasColumnName("target_department_id");
            e.Property(i => i.CreatedByUserId).HasColumnName("created_by_user_id");
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.HasOne(i => i.DebtDocument).WithMany(d => d.DebtItems)
             .HasForeignKey(i => i.DebtDocumentId).OnDelete(DeleteBehavior.Cascade);
            e.Property(i => i.LastRejectReason).HasColumnName("last_reject_reason").HasMaxLength(1000);
            e.Property(i => i.RejectedAt).HasColumnName("rejected_at");
            e.Property(i => i.RejectedByUserId).HasColumnName("rejected_by_user_id");
            e.Property(i => i.RejectedByUserName).HasColumnName("rejected_by_user_name").HasMaxLength(100);
            e.Property(i => i.RejectedByDepartmentCode).HasColumnName("rejected_by_department_code").HasMaxLength(20);
            e.HasOne(i => i.SupplementedByUser).WithMany()
             .HasForeignKey(i => i.SupplementedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<AppUser>().WithMany()
             .HasForeignKey(i => i.PqlnVerifiedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<AppUser>().WithMany()
             .HasForeignKey(i => i.PdvVerifiedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.SourceDepartment).WithMany()
             .HasForeignKey(i => i.SourceDepartmentId).OnDelete(DeleteBehavior.SetNull);
             e.HasOne(i => i.TargetDepartment).WithMany()
             .HasForeignKey(i => i.TargetDepartmentId).OnDelete(DeleteBehavior.SetNull);
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
            mb.Entity<WorkCalendarHoliday>(e =>
        {
            e.ToTable("work_calendar_holidays");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.HolidayDate).HasColumnName("holiday_date");
            e.Property(x => x.IsWorkingDay).HasColumnName("is_working_day");
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.HolidayDate).IsUnique();
        });

        mb.Entity<KpiConfig>(e =>
        {
            e.ToTable("kpi_configs");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            e.Property(x => x.MonthlyMaxCountPenalty).HasColumnName("monthly_max_count_penalty").HasPrecision(10, 4);
            e.Property(x => x.DelayPenaltyRate).HasColumnName("delay_penalty_rate").HasPrecision(10, 4);
            e.Property(x => x.DelayCycleDays).HasColumnName("delay_cycle_days");
            e.Property(x => x.DelayRoundingMode).HasColumnName("delay_rounding_mode").HasMaxLength(20);
            e.Property(x => x.CompletionMilestoneType).HasColumnName("completion_milestone_type").HasMaxLength(30);
            e.Property(x => x.SlaPkhDays).HasColumnName("sla_pkh_days");
            e.Property(x => x.SlaPqlnDays).HasColumnName("sla_pqln_days");
            e.Property(x => x.SlaPdvDays).HasColumnName("sla_pdv_days");
            e.Property(x => x.DirectPdvTotalSlaDays).HasColumnName("direct_pdv_total_sla_days");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        mb.Entity<KpiPeriod>(e =>
        {
            e.ToTable("kpi_periods");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Year).HasColumnName("year");
            e.Property(x => x.Month).HasColumnName("month");
            e.Property(x => x.WorkingDaysInMonth).HasColumnName("working_days_in_month");
            e.Property(x => x.CutoffDate).HasColumnName("cutoff_date");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
            e.Property(x => x.CalculatedAt).HasColumnName("calculated_at");
            e.Property(x => x.FinalizedAt).HasColumnName("finalized_at");
            e.Property(x => x.FinalizedByUserId).HasColumnName("finalized_by_user_id");
            e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
            e.HasIndex(x => new { x.Year, x.Month }).IsUnique();
        });

        mb.Entity<KpiViolationItem>(e =>
        {
            e.ToTable("kpi_violation_items");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.KpiPeriodId).HasColumnName("kpi_period_id");
            e.Property(x => x.DebtDocumentId).HasColumnName("debt_document_id");
            e.Property(x => x.DebtItemId).HasColumnName("debt_item_id");
            e.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(50);
            e.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(200);
            e.Property(x => x.ItemName).HasColumnName("item_name").HasMaxLength(255);
            e.Property(x => x.WorkflowType).HasColumnName("workflow_type").HasMaxLength(30);
            e.Property(x => x.IssueDate).HasColumnName("issue_date");
            e.Property(x => x.SlaDueDate).HasColumnName("sla_due_date");
            e.Property(x => x.ActualCompletedDate).HasColumnName("actual_completed_date");
            e.Property(x => x.PkhDays).HasColumnName("pkh_days");
            e.Property(x => x.PqlnDays).HasColumnName("pqln_days");
            e.Property(x => x.PdvDays).HasColumnName("pdv_days");
            e.Property(x => x.DelayWorkingDays).HasColumnName("delay_working_days");
            e.Property(x => x.IsCarriedOver).HasColumnName("is_carried_over");
            e.Property(x => x.AutoSuggestedDepartmentCode).HasColumnName("auto_suggested_department_code").HasMaxLength(20);
            e.Property(x => x.OverrideDepartmentCode).HasColumnName("override_department_code").HasMaxLength(20);
            e.Property(x => x.FinalDepartmentCode).HasColumnName("final_department_code").HasMaxLength(20);
            e.Property(x => x.IsExcluded).HasColumnName("is_excluded");
            e.Property(x => x.AdminNote).HasColumnName("admin_note").HasMaxLength(1000);
            e.HasOne(x => x.KpiPeriod).WithMany(p => p.Violations)
             .HasForeignKey(x => x.KpiPeriodId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.KpiPeriodId);
        });
        ConfigureSeedData(mb);
    }

    private static void ConfigureSeedData(ModelBuilder mb)
    {
        var pkhId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var pqlnId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var pdvId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        mb.Entity<KpiConfig>().HasData(new KpiConfig
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000100"),
            EffectiveFrom = new DateOnly(2026, 9, 1),
            MonthlyMaxCountPenalty = 0.25m,
            DelayPenaltyRate = 0.01m,
            DelayCycleDays = 5,
            DelayRoundingMode = "Linear",
            CompletionMilestoneType = "CompletedAt",
            SlaPkhDays = 6,
            SlaPqlnDays = 3,
            SlaPdvDays = 3,
            DirectPdvTotalSlaDays = 9,
            UpdatedAt = epoch
        });
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