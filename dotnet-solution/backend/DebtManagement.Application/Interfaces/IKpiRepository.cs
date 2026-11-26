using DebtManagement.Domain.Entities;

namespace DebtManagement.Application.Interfaces;

public interface IKpiRepository
{
    Task<List<WorkCalendarHoliday>> GetHolidaysAsync(int? year = null);
    Task<WorkCalendarHoliday?> GetHolidayByDateAsync(DateOnly date);
    Task AddHolidayAsync(WorkCalendarHoliday holiday);
    Task DeleteHolidayAsync(Guid id);

    Task<KpiConfig> GetOrCreateConfigAsync();
    Task SaveConfigAsync(KpiConfig config);

    Task<List<DebtDocument>> GetDocumentsForKpiAsync(DateOnly effectiveFrom);
    Task<KpiPeriod?> GetPeriodAsync(int year, int month);
    Task<List<KpiPeriod>> GetPeriodsInRangeAsync(int fromYear, int fromMonth, int toYear, int toMonth);
    Task SaveNewPeriodAsync(KpiPeriod period);
    Task ReplacePeriodViolationsAsync(KpiPeriod period, List<KpiViolationItem> newViolations);
    Task<KpiViolationItem?> GetViolationItemAsync(Guid violationId);
    Task UpdateViolationItemAsync(KpiViolationItem item);
    Task UpdatePeriodAsync(KpiPeriod period);
}