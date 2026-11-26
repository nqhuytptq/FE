using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using DebtManagement.Shared.Constants;
using DebtManagement.Shared.DTOs;

namespace DebtManagement.Application.Services;

public class WorkingCalendarService(
    IKpiRepository kpiRepo,
    IUserRepository users) : IWorkingCalendarService
{
    private async Task EnsureAdminAsync(Guid userId)
    {
        var user = await users.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Tài khoản không tồn tại");
        if (user.Role != WorkflowConstants.RoleAdmin)
            throw new UnauthorizedAccessException("Chỉ Quản trị viên (Admin) mới được thực hiện thao tác này");
    }

    public async Task<List<WorkCalendarHolidayDto>> GetHolidaysAsync(int? year = null)
    {
        var list = await kpiRepo.GetHolidaysAsync(year);
        return list.Select(x => new WorkCalendarHolidayDto(x.Id, x.HolidayDate, x.IsWorkingDay, x.Description)).ToList();
    }

    public async Task<WorkCalendarHolidayDto> UpsertHolidayAsync(UpsertHolidayRequest request, Guid adminUserId)
    {
        await EnsureAdminAsync(adminUserId);
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new InvalidOperationException("Vui lòng nhập mô tả ngày lễ / làm bù");

        var existing = await kpiRepo.GetHolidayByDateAsync(request.HolidayDate);
        if (existing != null)
            await kpiRepo.DeleteHolidayAsync(existing.Id);

        var entity = new WorkCalendarHoliday
        {
            HolidayDate = request.HolidayDate,
            IsWorkingDay = request.IsWorkingDay,
            Description = request.Description.Trim()
        };
        await kpiRepo.AddHolidayAsync(entity);
        return new WorkCalendarHolidayDto(entity.Id, entity.HolidayDate, entity.IsWorkingDay, entity.Description);
    }

    public async Task DeleteHolidayAsync(Guid id, Guid adminUserId)
    {
        await EnsureAdminAsync(adminUserId);
        await kpiRepo.DeleteHolidayAsync(id);
    }

    public bool IsWorkingDay(DateOnly date, IReadOnlyDictionary<DateOnly, bool> overrides)
    {
        if (overrides.TryGetValue(date, out var isWork))
            return isWork;
        return date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
    }

    public List<DateOnly> GetWorkingDaysInMonth(int year, int month, IReadOnlyDictionary<DateOnly, bool> overrides)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var result = new List<DateOnly>(23);
        for (int day = 1; day <= daysInMonth; day++)
        {
            var d = new DateOnly(year, month, day);
            if (IsWorkingDay(d, overrides))
                result.Add(d);
        }
        return result;
    }

    public DateOnly GetCutoffDate(int year, int month, int totalSlaDays, IReadOnlyDictionary<DateOnly, bool> overrides)
    {
        var workDays = GetWorkingDaysInMonth(year, month, overrides);
        if (workDays.Count == 0)
            return new DateOnly(year, month, 1);
        var idx = Math.Max(0, workDays.Count - 1 - totalSlaDays);
        return workDays[idx];
    }

    public DateOnly AddWorkingDays(DateOnly start, int workingDays, IReadOnlyDictionary<DateOnly, bool> overrides)
    {
        if (workingDays <= 0) return start;
        var current = start;
        var remaining = workingDays;
        while (remaining > 0)
        {
            current = current.AddDays(1);
            if (IsWorkingDay(current, overrides))
                remaining--;
        }
        return current;
    }

    public int CountWorkingDaysBetween(DateOnly startExclusive, DateOnly endInclusive, IReadOnlyDictionary<DateOnly, bool> overrides)
    {
        if (endInclusive <= startExclusive) return 0;
        var count = 0;
        var current = startExclusive.AddDays(1);
        while (current <= endInclusive)
        {
            if (IsWorkingDay(current, overrides))
                count++;
            current = current.AddDays(1);
        }
        return count;
    }
}