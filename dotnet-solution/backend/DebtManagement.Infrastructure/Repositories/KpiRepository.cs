using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using DebtManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DebtManagement.Infrastructure.Repositories;

public class KpiRepository(AppDbContext db) : IKpiRepository
{
    public async Task<List<WorkCalendarHoliday>> GetHolidaysAsync(int? year = null)
    {
        var query = db.WorkCalendarHolidays.AsNoTracking();
        if (year.HasValue)
            query = query.Where(x => x.HolidayDate.Year == year.Value);
        return await query.OrderBy(x => x.HolidayDate).ToListAsync();
    }

    public Task<WorkCalendarHoliday?> GetHolidayByDateAsync(DateOnly date)
        => db.WorkCalendarHolidays.FirstOrDefaultAsync(x => x.HolidayDate == date);

    public async Task AddHolidayAsync(WorkCalendarHoliday holiday)
    {
        db.WorkCalendarHolidays.Add(holiday);
        await db.SaveChangesAsync();
    }

    public async Task DeleteHolidayAsync(Guid id)
    {
        var entity = await db.WorkCalendarHolidays.FindAsync(id);
        if (entity != null)
        {
            db.WorkCalendarHolidays.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task<KpiConfig> GetOrCreateConfigAsync()
    {
        var cfg = await db.KpiConfigs.FirstOrDefaultAsync();
        if (cfg != null) return cfg;

        cfg = new KpiConfig();
        db.KpiConfigs.Add(cfg);
        await db.SaveChangesAsync();
        return cfg;
    }

    public async Task SaveConfigAsync(KpiConfig config)
    {
        db.KpiConfigs.Update(config);
        await db.SaveChangesAsync();
    }

public Task<List<DebtDocument>> GetDocumentsForKpiAsync(DateOnly effectiveFrom)
        => db.DebtDocuments
            .AsNoTracking()
            .Include(d => d.DebtItems)
                .ThenInclude(i => i.SourceDepartment)
            .Include(d => d.DebtItems)
                .ThenInclude(i => i.TargetDepartment)
            .Where(d => d.IssueDate >= effectiveFrom)
            .OrderBy(d => d.IssueDate)
            .ToListAsync();

    public Task<KpiPeriod?> GetPeriodAsync(int year, int month)
        => db.KpiPeriods
            .Include(p => p.Violations.OrderBy(v => v.IssueDate).ThenBy(v => v.DocumentNumber))
            .FirstOrDefaultAsync(p => p.Year == year && p.Month == month);

    public Task<List<KpiPeriod>> GetPeriodsInRangeAsync(int fromYear, int fromMonth, int toYear, int toMonth)
    {
        var startKey = fromYear * 100 + fromMonth;
        var endKey = toYear * 100 + toMonth;
        return db.KpiPeriods
            .AsNoTracking()
            .Include(p => p.Violations)
            .Where(p => (p.Year * 100 + p.Month) >= startKey && (p.Year * 100 + p.Month) <= endKey)
            .OrderBy(p => p.Year)
            .ThenBy(p => p.Month)
            .ToListAsync();
    }

    public async Task SaveNewPeriodAsync(KpiPeriod period)
    {
        db.KpiPeriods.Add(period);
        await db.SaveChangesAsync();
    }

    public async Task ReplacePeriodViolationsAsync(KpiPeriod period, List<KpiViolationItem> newViolations)
    {
        var oldItems = await db.KpiViolationItems.Where(x => x.KpiPeriodId == period.Id).ToListAsync();
        if (oldItems.Count > 0)
            db.KpiViolationItems.RemoveRange(oldItems);

        db.KpiViolationItems.AddRange(newViolations);
        db.KpiPeriods.Update(period);
        await db.SaveChangesAsync();
    }

    public Task<KpiViolationItem?> GetViolationItemAsync(Guid violationId)
        => db.KpiViolationItems
            .Include(v => v.KpiPeriod)
            .FirstOrDefaultAsync(v => v.Id == violationId);

    public async Task UpdateViolationItemAsync(KpiViolationItem item)
    {
        db.KpiViolationItems.Update(item);
        await db.SaveChangesAsync();
    }

    public async Task UpdatePeriodAsync(KpiPeriod period)
    {
        db.KpiPeriods.Update(period);
        await db.SaveChangesAsync();
    }
}