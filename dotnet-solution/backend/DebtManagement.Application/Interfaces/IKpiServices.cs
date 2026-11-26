using DebtManagement.Shared.DTOs;

namespace DebtManagement.Application.Interfaces;

public interface IWorkingCalendarService
{
    Task<List<WorkCalendarHolidayDto>> GetHolidaysAsync(int? year = null);
    Task<WorkCalendarHolidayDto> UpsertHolidayAsync(UpsertHolidayRequest request, Guid adminUserId);
    Task DeleteHolidayAsync(Guid id, Guid adminUserId);

    bool IsWorkingDay(DateOnly date, IReadOnlyDictionary<DateOnly, bool> overrides);
    List<DateOnly> GetWorkingDaysInMonth(int year, int month, IReadOnlyDictionary<DateOnly, bool> overrides);
    DateOnly GetCutoffDate(int year, int month, int totalSlaDays, IReadOnlyDictionary<DateOnly, bool> overrides);
    DateOnly AddWorkingDays(DateOnly start, int workingDays, IReadOnlyDictionary<DateOnly, bool> overrides);
    int CountWorkingDaysBetween(DateOnly startExclusive, DateOnly endInclusive, IReadOnlyDictionary<DateOnly, bool> overrides);
}

public interface IKpiCalculationService
{
    Task<KpiConfigDto> GetConfigAsync();
    Task<KpiConfigDto> UpdateConfigAsync(UpdateKpiConfigRequest request, Guid adminUserId);

    Task<KpiPeriodDetailDto?> GetPeriodAsync(int year, int month);
    Task<KpiPeriodDetailDto> CalculateOrRefreshPeriodAsync(int year, int month, Guid adminUserId);
    Task<KpiPeriodDetailDto> UpdateViolationOverrideAsync(Guid violationId, UpdateViolationOverrideRequest request, Guid adminUserId);
    Task<KpiPeriodDetailDto> FinalizePeriodAsync(int year, int month, string? notes, Guid adminUserId);
    Task<KpiPeriodDetailDto> ReopenPeriodAsync(int year, int month, Guid adminUserId);

    Task<KpiCumulativeReportDto> GetCumulativeReportAsync(int fromYear, int fromMonth, int toYear, int toMonth);
    Task<byte[]> ExportCumulativeExcelAsync(int fromYear, int fromMonth, int toYear, int toMonth);
}