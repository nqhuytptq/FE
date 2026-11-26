using System.Security.Claims;
using DebtManagement.Application.Interfaces;
using DebtManagement.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DebtManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class KpiController(
    IKpiCalculationService kpiService,
    IWorkingCalendarService calendarService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
        => Ok(await kpiService.GetConfigAsync());

    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateKpiConfigRequest request)
        => Ok(await kpiService.UpdateConfigAsync(request, CurrentUserId));

    [HttpGet("holidays")]
    public async Task<IActionResult> GetHolidays([FromQuery] int? year)
        => Ok(await calendarService.GetHolidaysAsync(year));

    [HttpPost("holidays")]
    public async Task<IActionResult> UpsertHoliday([FromBody] UpsertHolidayRequest request)
        => Ok(await calendarService.UpsertHolidayAsync(request, CurrentUserId));

    [HttpDelete("holidays/{id:guid}")]
    public async Task<IActionResult> DeleteHoliday(Guid id)
    {
        await calendarService.DeleteHolidayAsync(id, CurrentUserId);
        return NoContent();
    }

    [HttpGet("periods/{year:int}/{month:int}")]
    public async Task<IActionResult> GetPeriod(int year, int month)
        => Ok(await kpiService.GetPeriodAsync(year, month));

    [HttpPost("periods/{year:int}/{month:int}/calculate")]
    public async Task<IActionResult> CalculatePeriod(int year, int month)
        => Ok(await kpiService.CalculateOrRefreshPeriodAsync(year, month, CurrentUserId));

    [HttpPut("violations/{violationId:guid}")]
    public async Task<IActionResult> UpdateViolationOverride(Guid violationId, [FromBody] UpdateViolationOverrideRequest request)
        => Ok(await kpiService.UpdateViolationOverrideAsync(violationId, request, CurrentUserId));

    [HttpPost("periods/{year:int}/{month:int}/finalize")]
    public async Task<IActionResult> FinalizePeriod(int year, int month, [FromBody] string? notes)
        => Ok(await kpiService.FinalizePeriodAsync(year, month, notes, CurrentUserId));

    [HttpPost("periods/{year:int}/{month:int}/reopen")]
    public async Task<IActionResult> ReopenPeriod(int year, int month)
        => Ok(await kpiService.ReopenPeriodAsync(year, month, CurrentUserId));

    [HttpGet("report")]
    public async Task<IActionResult> GetReport(
        [FromQuery] int fromYear, [FromQuery] int fromMonth,
        [FromQuery] int toYear, [FromQuery] int toMonth)
        => Ok(await kpiService.GetCumulativeReportAsync(fromYear, fromMonth, toYear, toMonth));

    [HttpGet("export")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] int fromYear, [FromQuery] int fromMonth,
        [FromQuery] int toYear, [FromQuery] int toMonth)
    {
        var bytes = await kpiService.ExportCumulativeExcelAsync(fromYear, fromMonth, toYear, toMonth);
        var fileName = $"KPI_ChungTuNo_{fromYear}{fromMonth:D2}_{toYear}{toMonth:D2}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}