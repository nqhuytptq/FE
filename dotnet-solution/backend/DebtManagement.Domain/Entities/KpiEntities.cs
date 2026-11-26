namespace DebtManagement.Domain.Entities;

public class WorkCalendarHoliday
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly HolidayDate { get; set; }
    public bool IsWorkingDay { get; set; } // false = ngày nghỉ lễ; true = ngày làm bù (T7/CN)
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class KpiConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly EffectiveFrom { get; set; } = new(2026, 9, 1);
    public decimal MonthlyMaxCountPenalty { get; set; } = 0.25m;
    public decimal DelayPenaltyRate { get; set; } = 0.01m;
    public int DelayCycleDays { get; set; } = 5;
    public string DelayRoundingMode { get; set; } = "Linear"; // Linear | Floor | Ceiling
    public string CompletionMilestoneType { get; set; } = "CompletedAt"; // CompletedAt | EntryPostedAt
    public int SlaPkhDays { get; set; } = 6;
    public int SlaPqlnDays { get; set; } = 3;
    public int SlaPdvDays { get; set; } = 3;
    public int DirectPdvTotalSlaDays { get; set; } = 9;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class KpiPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    public int Month { get; set; }
    public int WorkingDaysInMonth { get; set; }
    public DateOnly CutoffDate { get; set; }
    public string Status { get; set; } = "Draft"; // Draft | Finalized
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizedAt { get; set; }
    public Guid? FinalizedByUserId { get; set; }
    public string? Notes { get; set; }

    public ICollection<KpiViolationItem> Violations { get; set; } = new List<KpiViolationItem>();
}

public class KpiViolationItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid KpiPeriodId { get; set; }
    public Guid DebtDocumentId { get; set; }
    public Guid? DebtItemId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = "pkh_pqln_pdv";
    public DateOnly IssueDate { get; set; }
    public DateOnly SlaDueDate { get; set; }
    public DateOnly? ActualCompletedDate { get; set; }
    public int PkhDays { get; set; }
    public int PqlnDays { get; set; }
    public int PdvDays { get; set; }
    public int DelayWorkingDays { get; set; }
    public bool IsCarriedOver { get; set; }
    public string AutoSuggestedDepartmentCode { get; set; } = string.Empty;
    public string? OverrideDepartmentCode { get; set; }
    public string FinalDepartmentCode { get; set; } = string.Empty;
    public bool IsExcluded { get; set; }
    public string? AdminNote { get; set; }

    public KpiPeriod? KpiPeriod { get; set; }
}