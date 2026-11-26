namespace DebtManagement.Shared.DTOs;

public record WorkCalendarHolidayDto(
    Guid Id,
    DateOnly HolidayDate,
    bool IsWorkingDay,
    string Description);

public record UpsertHolidayRequest(
    DateOnly HolidayDate,
    bool IsWorkingDay,
    string Description);

public record KpiConfigDto(
    Guid Id,
    DateOnly EffectiveFrom,
    decimal MonthlyMaxCountPenalty,
    decimal DelayPenaltyRate,
    int DelayCycleDays,
    string DelayRoundingMode,
    string CompletionMilestoneType,
    int SlaPkhDays,
    int SlaPqlnDays,
    int SlaPdvDays,
    int DirectPdvTotalSlaDays,
    DateTime UpdatedAt);

public record UpdateKpiConfigRequest(
    DateOnly EffectiveFrom,
    decimal MonthlyMaxCountPenalty,
    decimal DelayPenaltyRate,
    int DelayCycleDays,
    string DelayRoundingMode,
    string CompletionMilestoneType,
    int SlaPkhDays,
    int SlaPqlnDays,
    int SlaPdvDays,
    int DirectPdvTotalSlaDays);

public record KpiViolationItemDto(
    Guid Id,
    Guid DebtDocumentId,
    Guid? DebtItemId,
    string DocumentNumber,
    string CustomerName,
    string ItemName,
    string WorkflowType,
    DateOnly IssueDate,
    DateOnly SlaDueDate,
    DateOnly? ActualCompletedDate,
    int PkhDays,
    int PqlnDays,
    int PdvDays,
    int DelayWorkingDays,
    decimal CalculatedDelayPenalty,
    bool IsCarriedOver,
    string AutoSuggestedDepartmentCode,
    string? OverrideDepartmentCode,
    string FinalDepartmentCode,
    bool IsExcluded,
    string? AdminNote);

public record UpdateViolationOverrideRequest(
    string? OverrideDepartmentCode,
    bool IsExcluded,
    string? AdminNote);

public record DepartmentKpiScoreDto(
    string DepartmentCode,
    string DepartmentName,
    int DistinctLateDays,
    int WorkingDaysInMonth,
    decimal CountPenalty,
    int ViolationItemCount,
    int TotalDelayWorkingDays,
    decimal DelayPenalty,
    decimal TotalPenalty);

public record KpiPeriodDetailDto(
    Guid Id,
    int Year,
    int Month,
    int WorkingDaysInMonth,
    DateOnly CutoffDate,
    string Status,
    DateTime CalculatedAt,
    DateTime? FinalizedAt,
    string? Notes,
    List<DepartmentKpiScoreDto> DepartmentScores,
    List<KpiViolationItemDto> Violations);

public record KpiMonthSummaryDto(
    int Year,
    int Month,
    string Status,
    int WorkingDaysInMonth,
    List<DepartmentKpiScoreDto> DepartmentScores);

public record KpiCumulativeReportDto(
    int FromYear,
    int FromMonth,
    int ToYear,
    int ToMonth,
    List<DepartmentKpiScoreDto> CumulativeScores,
    List<KpiMonthSummaryDto> MonthlyBreakdown);