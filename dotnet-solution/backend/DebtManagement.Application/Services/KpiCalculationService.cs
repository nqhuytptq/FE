using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using DebtManagement.Shared.Constants;
using DebtManagement.Shared.DTOs;
using static DebtManagement.Application.Services.DebtDocumentRules;

namespace DebtManagement.Application.Services;

public class KpiCalculationService(
    IKpiRepository kpiRepo,
    IWorkingCalendarService calendar,
    IUserRepository users) : IKpiCalculationService
{
    private static readonly (string Code, string Name)[] TrackedDepartments =
    [
        (WorkflowConstants.DeptKhdn, "Phòng Khách hàng Doanh nghiệp"),
        (WorkflowConstants.DeptKhbl, "Phòng Khách hàng Bán lẻ"),
        (WorkflowConstants.DeptPgdLlq, "PGD Lạc Long Quân"),
        (WorkflowConstants.DeptPgdCd, "PGD Cửa Đông"),
        (WorkflowConstants.DeptPqln, "Phòng Quản lý nợ"),
        (WorkflowConstants.DeptPdv, "Phòng Dịch vụ khách hàng")
    ];

    private static IEnumerable<(string Code, string Name)> GetEffectiveDepartments(IEnumerable<KpiViolationItem> violations)
    {
        var list = TrackedDepartments.ToList();
        if (violations.Any(v => string.Equals(v.FinalDepartmentCode, WorkflowConstants.DeptPkh, StringComparison.OrdinalIgnoreCase)))
        {
            list.Add((WorkflowConstants.DeptPkh, "Phòng Khách hàng (Lịch sử)"));
        }
        return list;
    }

    private async Task EnsureAdminAsync(Guid userId)
    {
        var user = await users.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Tài khoản không tồn tại");
        if (!string.Equals(user.Role, WorkflowConstants.RoleAdmin, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Chỉ Quản trị viên (Admin) mới được thực hiện thao tác này");
    }

    private static DateOnly ToVnDate(DateTime dt)
    {
        var vn = dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime().AddHours(7) : dt.AddHours(7);
        return DateOnly.FromDateTime(vn);
    }

    private async Task<Dictionary<DateOnly, bool>> LoadHolidayMapAsync()
    {
        var holidays = await kpiRepo.GetHolidaysAsync();
        return holidays.ToDictionary(x => x.HolidayDate, x => x.IsWorkingDay);
    }

    public async Task<KpiConfigDto> GetConfigAsync()
        => MapConfig(await kpiRepo.GetOrCreateConfigAsync());

    public async Task<KpiConfigDto> UpdateConfigAsync(UpdateKpiConfigRequest r, Guid adminUserId)
    {
        await EnsureAdminAsync(adminUserId);
        if (r.DelayCycleDays <= 0) throw new InvalidOperationException("Chu kỳ ngày phải > 0");
        if (r.SlaPkhDays <= 0 || r.SlaPqlnDays <= 0 || r.SlaPdvDays <= 0 || r.DirectPdvTotalSlaDays <= 0)
            throw new InvalidOperationException("Số ngày SLA phải > 0");

        var cfg = await kpiRepo.GetOrCreateConfigAsync();
        cfg.EffectiveFrom = r.EffectiveFrom;
        cfg.MonthlyMaxCountPenalty = r.MonthlyMaxCountPenalty;
        cfg.DelayPenaltyRate = r.DelayPenaltyRate;
        cfg.DelayCycleDays = r.DelayCycleDays;
        cfg.DelayRoundingMode = r.DelayRoundingMode is "Floor" or "Ceiling" ? r.DelayRoundingMode : "Linear";
        cfg.CompletionMilestoneType = r.CompletionMilestoneType == "EntryPostedAt" ? "EntryPostedAt" : "CompletedAt";
        cfg.SlaPkhDays = r.SlaPkhDays;
        cfg.SlaPqlnDays = r.SlaPqlnDays;
        cfg.SlaPdvDays = r.SlaPdvDays;
        cfg.DirectPdvTotalSlaDays = r.DirectPdvTotalSlaDays;
        cfg.UpdatedAt = DateTime.UtcNow;

        await kpiRepo.SaveConfigAsync(cfg);
        return MapConfig(cfg);
    }

    public async Task<KpiPeriodDetailDto?> GetPeriodAsync(int year, int month)
    {
        var period = await kpiRepo.GetPeriodAsync(year, month);
        if (period == null) return null;
        var cfg = await kpiRepo.GetOrCreateConfigAsync();
        return BuildPeriodDto(period, cfg);
    }

    public async Task<KpiPeriodDetailDto> CalculateOrRefreshPeriodAsync(int year, int month, Guid adminUserId)
    {
        await EnsureAdminAsync(adminUserId);

        var cfg = await kpiRepo.GetOrCreateConfigAsync();
        var holidayMap = await LoadHolidayMapAsync();
        var existingPeriod = await kpiRepo.GetPeriodAsync(year, month);

        if (existingPeriod?.Status == "Finalized")
            throw new InvalidOperationException($"Kỳ {month:D2}/{year} đã chốt. Vui lòng mở lại kỳ trước khi tính toán lại.");

        var preservedOverrides = existingPeriod?.Violations
            .ToDictionary(
                v => (v.DebtDocumentId, v.DebtItemId),
                v => (v.OverrideDepartmentCode, v.IsExcluded, v.AdminNote))
            ?? new();

        var workDays = calendar.GetWorkingDaysInMonth(year, month, holidayMap);
        var workingDaysInMonth = workDays.Count;
        var totalStandardSla = cfg.SlaPkhDays + cfg.SlaPqlnDays + cfg.SlaPdvDays;
        var cutoffDate = calendar.GetCutoffDate(year, month, totalStandardSla, holidayMap);

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var prevPeriodEnd = periodStart.AddDays(-1);

        var docs = await kpiRepo.GetDocumentsForKpiAsync(cfg.EffectiveFrom);
        var periodId = existingPeriod?.Id ?? Guid.NewGuid();
        var newViolations = new List<KpiViolationItem>();

        foreach (var doc in docs)
        {
            if (!doc.SupPdvApprovedEntryAt.HasValue) continue;
            if (doc.DebtItems.Count == 0) continue;

            var supPdvApproveDate = ToVnDate(doc.SupPdvApprovedEntryAt.Value);
            if (supPdvApproveDate > periodEnd) continue;

            foreach (var item in doc.DebtItems)
            {
                var violation = EvaluateDebtItemFromSupPdvApproval(
                    periodId, doc, item, supPdvApproveDate,
                    periodStart, periodEnd, prevPeriodEnd, holidayMap, preservedOverrides);
                if (violation != null)
                    newViolations.Add(violation);
            }
        }

        if (existingPeriod == null)
        {
            var newPeriod = new KpiPeriod
            {
                Id = periodId,
                Year = year,
                Month = month,
                WorkingDaysInMonth = workingDaysInMonth,
                CutoffDate = cutoffDate,
                Status = "Draft",
                CalculatedAt = DateTime.UtcNow,
                Violations = newViolations
            };
            await kpiRepo.SaveNewPeriodAsync(newPeriod);
            return BuildPeriodDto(newPeriod, cfg);
        }
        else
        {
            existingPeriod.WorkingDaysInMonth = workingDaysInMonth;
            existingPeriod.CutoffDate = cutoffDate;
            existingPeriod.CalculatedAt = DateTime.UtcNow;
            await kpiRepo.ReplacePeriodViolationsAsync(existingPeriod, newViolations);

            var reloaded = await kpiRepo.GetPeriodAsync(year, month) ?? existingPeriod;
            return BuildPeriodDto(reloaded, cfg);
        }
    }

    private KpiViolationItem? EvaluateDebtItemFromSupPdvApproval(
        Guid periodId,
        DebtDocument doc,
        DebtItem item,
        DateOnly supPdvApproveDate,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly prevPeriodEnd,
        IReadOnlyDictionary<DateOnly, bool> holidayMap,
        Dictionary<(Guid, Guid?), (string? OverrideDept, bool IsExcluded, string? AdminNote)> preservedOverrides)
    {
        var fallbackOrigin = GetOriginDepartmentCode(doc);
        var sourceCode = GetSourceDepartmentCode(item, null, null, fallbackOrigin);
        var targetCode = GetTargetDepartmentCode(item, doc.WorkflowType);
        var actualCompletedDate = ResolveDebtItemCompletedDate(item, targetCode);

        if (actualCompletedDate.HasValue && actualCompletedDate.Value <= supPdvApproveDate)
            return null;

        var isCarriedOver = supPdvApproveDate < periodStart;
        if (isCarriedOver && actualCompletedDate.HasValue && actualCompletedDate.Value < periodStart)
            return null;

        var evalEnd = actualCompletedDate.HasValue && actualCompletedDate.Value <= periodEnd
            ? actualCompletedDate.Value
            : periodEnd;

        var delayDays = isCarriedOver
            ? calendar.CountWorkingDaysBetween(prevPeriodEnd, evalEnd, holidayMap)
            : calendar.CountWorkingDaysBetween(supPdvApproveDate, evalEnd, holidayMap);

        if (delayDays <= 0)
            return null;

        var (pkhDays, pqlnDays, pdvDays) = ComputeStageReferenceDays(doc, item, sourceCode, supPdvApproveDate, evalEnd, holidayMap);

        var key = (doc.Id, (Guid?)item.Id);
        preservedOverrides.TryGetValue(key, out var prev);

        var autoDept = sourceCode;
        var finalDept = string.IsNullOrWhiteSpace(prev.OverrideDept) ? autoDept : prev.OverrideDept!;

        return new KpiViolationItem
        {
            KpiPeriodId = periodId,
            DebtDocumentId = doc.Id,
            DebtItemId = item.Id,
            DocumentNumber = doc.DocumentNumber,
            CustomerName = doc.CustomerName,
            ItemName = $"{item.ItemName} (Nợ {targetCode})",
            WorkflowType = doc.WorkflowType,
            IssueDate = doc.IssueDate,
            SlaDueDate = supPdvApproveDate,
            ActualCompletedDate = actualCompletedDate,
            PkhDays = pkhDays,
            PqlnDays = pqlnDays,
            PdvDays = pdvDays,
            DelayWorkingDays = delayDays,
            IsCarriedOver = isCarriedOver,
            AutoSuggestedDepartmentCode = autoDept,
            OverrideDepartmentCode = prev.OverrideDept,
            FinalDepartmentCode = finalDept,
            IsExcluded = prev.IsExcluded,
            AdminNote = prev.AdminNote
        };
    }

    private static DateOnly? ResolveDebtItemCompletedDate(DebtItem item, string targetCode)
    {
        if (!item.IsSupplemented) return null;

        if (targetCode == "PDV")
            return item.IsVerifiedByPdv && item.PdvVerifiedAt.HasValue ? ToVnDate(item.PdvVerifiedAt.Value) : null;

        return item.IsVerifiedByPqln && item.PqlnVerifiedAt.HasValue ? ToVnDate(item.PqlnVerifiedAt.Value) : null;
    }

    private (int PkhDays, int PqlnDays, int PdvDays) ComputeStageReferenceDays(
        DebtDocument doc,
        DebtItem item,
        string sourceCode,
        DateOnly supPdvApproveDate,
        DateOnly evalEnd,
        IReadOnlyDictionary<DateOnly, bool> holidayMap)
    {
        var suppDate = item.SupplementedAt.HasValue ? ToVnDate(item.SupplementedAt.Value) : (DateOnly?)null;

        var borrowerEnd = suppDate.HasValue && suppDate.Value < evalEnd ? suppDate.Value : evalEnd;
        var borrowerDebtDays = borrowerEnd > supPdvApproveDate
            ? calendar.CountWorkingDaysBetween(supPdvApproveDate, borrowerEnd, holidayMap)
            : 0;

        var verifierWaitDays = 0;
        if (suppDate.HasValue && evalEnd > suppDate.Value)
        {
            var verifyStart = suppDate.Value > supPdvApproveDate ? suppDate.Value : supPdvApproveDate;
            verifierWaitDays = calendar.CountWorkingDaysBetween(verifyStart, evalEnd, holidayMap);
        }

        var targetCode = GetTargetDepartmentCode(item, doc.WorkflowType);
        var pkhDays = WorkflowConstants.IsSalesDept(sourceCode) ? borrowerDebtDays : 0;
        var pqlnDays = sourceCode == "PQLN" ? borrowerDebtDays : (targetCode == "PQLN" ? verifierWaitDays : 0);
        var pdvDays = targetCode == "PDV" ? verifierWaitDays : 0;

        return (pkhDays, pqlnDays, pdvDays);
    }

    public async Task<KpiPeriodDetailDto> UpdateViolationOverrideAsync(Guid violationId, UpdateViolationOverrideRequest request, Guid adminUserId)
    {
        await EnsureAdminAsync(adminUserId);
        var item = await kpiRepo.GetViolationItemAsync(violationId)
            ?? throw new InvalidOperationException("Không tìm thấy dòng vi phạm");

        if (item.KpiPeriod?.Status == "Finalized")
            throw new InvalidOperationException("Kỳ KPI đã chốt, không thể chỉnh sửa đối soát");

        item.OverrideDepartmentCode = string.IsNullOrWhiteSpace(request.OverrideDepartmentCode)
            ? null
            : request.OverrideDepartmentCode.Trim().ToUpperInvariant();
        item.FinalDepartmentCode = item.OverrideDepartmentCode ?? item.AutoSuggestedDepartmentCode;
        item.IsExcluded = request.IsExcluded;
        item.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();

        await kpiRepo.UpdateViolationItemAsync(item);

        var cfg = await kpiRepo.GetOrCreateConfigAsync();
        var period = await kpiRepo.GetPeriodAsync(item.KpiPeriod!.Year, item.KpiPeriod.Month)
            ?? throw new InvalidOperationException("Không tìm thấy kỳ KPI");
        return BuildPeriodDto(period, cfg);
    }

    public async Task<KpiPeriodDetailDto> FinalizePeriodAsync(int year, int month, string? notes, Guid adminUserId)
    {
        await EnsureAdminAsync(adminUserId);
        var period = await kpiRepo.GetPeriodAsync(year, month)
            ?? throw new InvalidOperationException("Vui lòng tính toán dữ liệu kỳ trước khi chốt");

        period.Status = "Finalized";
        period.FinalizedAt = DateTime.UtcNow;
        period.FinalizedByUserId = adminUserId;
        if (!string.IsNullOrWhiteSpace(notes))
            period.Notes = notes.Trim();

        await kpiRepo.UpdatePeriodAsync(period);
        var cfg = await kpiRepo.GetOrCreateConfigAsync();
        return BuildPeriodDto(period, cfg);
    }

    public async Task<KpiPeriodDetailDto> ReopenPeriodAsync(int year, int month, Guid adminUserId)
    {
        await EnsureAdminAsync(adminUserId);
        var period = await kpiRepo.GetPeriodAsync(year, month)
            ?? throw new InvalidOperationException("Không tìm thấy kỳ KPI");

        period.Status = "Draft";
        period.FinalizedAt = null;
        period.FinalizedByUserId = null;

        await kpiRepo.UpdatePeriodAsync(period);
        var cfg = await kpiRepo.GetOrCreateConfigAsync();
        return BuildPeriodDto(period, cfg);
    }

    public async Task<KpiCumulativeReportDto> GetCumulativeReportAsync(int fromYear, int fromMonth, int toYear, int toMonth)
    {
        var cfg = await kpiRepo.GetOrCreateConfigAsync();
        var periods = await kpiRepo.GetPeriodsInRangeAsync(fromYear, fromMonth, toYear, toMonth);

        var monthlyBreakdown = periods
            .Select(p => new KpiMonthSummaryDto(p.Year, p.Month, p.Status, p.WorkingDaysInMonth, ComputeDepartmentScores(p, cfg)))
            .ToList();

        var allViolations = periods.SelectMany(p => p.Violations);
        var cumulative = GetEffectiveDepartments(allViolations).Select(dept =>
        {
            var deptMonths = monthlyBreakdown
                .SelectMany(m => m.DepartmentScores)
                .Where(s => s.DepartmentCode == dept.Code)
                .ToList();

            var countPenalty = deptMonths.Sum(x => x.CountPenalty);
            var delayPenalty = deptMonths.Sum(x => x.DelayPenalty);

            return new DepartmentKpiScoreDto(
                dept.Code,
                dept.Name,
                deptMonths.Sum(x => x.DistinctLateDays),
                deptMonths.Sum(x => x.WorkingDaysInMonth),
                countPenalty,
                deptMonths.Sum(x => x.ViolationItemCount),
                deptMonths.Sum(x => x.TotalDelayWorkingDays),
                delayPenalty,
                countPenalty + delayPenalty);
        }).ToList();

        return new KpiCumulativeReportDto(fromYear, fromMonth, toYear, toMonth, cumulative, monthlyBreakdown);
    }

    public async Task<byte[]> ExportCumulativeExcelAsync(int fromYear, int fromMonth, int toYear, int toMonth)
    {
        var cfg = await kpiRepo.GetOrCreateConfigAsync();
        var report = await GetCumulativeReportAsync(fromYear, fromMonth, toYear, toMonth);
        var periods = await kpiRepo.GetPeriodsInRangeAsync(fromYear, fromMonth, toYear, toMonth);

        var summaryRows = new List<string[]>
        {
            new[] { $"BÁO CÁO ĐIỂM TRỪ KPI CHỨNG TỪ NỢ ({fromMonth:D2}/{fromYear} - {toMonth:D2}/{toYear})" },
            new[] { "Mã phòng", "Tên phòng", "Số ngày phát sinh nợ (II.1)", "Tổng ngày làm việc", "Điểm trừ SL chứng từ (II.1)", "Số chứng từ nợ (II.2)", "Tổng ngày nợ", "Điểm trừ thời gian nợ (II.2)", "Tổng điểm trừ" }
        };

        foreach (var d in report.CumulativeScores)
        {
            summaryRows.Add(
            [
                d.DepartmentCode,
                d.DepartmentName,
                d.DistinctLateDays.ToString(),
                d.WorkingDaysInMonth.ToString(),
                d.CountPenalty.ToString("0.####"),
                d.ViolationItemCount.ToString(),
                d.TotalDelayWorkingDays.ToString(),
                d.DelayPenalty.ToString("0.####"),
                d.TotalPenalty.ToString("0.####")
            ]);
        }

        var detailRows = new List<string[]>
        {
            new[] { "Kỳ", "Số văn bản", "Khách hàng", "Chứng từ nợ", "Luồng", "Ngày tạo HS", "Ngày SUP PDV duyệt BT", "Ngày trả xong CT", "Ngày nợ của phòng nợ", "Ngày chờ xác nhận", "Số ngày nợ tính KPI", "Điểm trừ II.2", "Phòng chịu lỗi", "Chuyển tiếp kỳ trước", "Loại trừ", "Ghi chú" }
        };

        foreach (var p in periods)
        {
            foreach (var v in p.Violations.OrderBy(x => x.SlaDueDate))
            {
                var penalty = v.IsExcluded ? 0m : CalculateSingleItemDelayPenalty(v.DelayWorkingDays, cfg);
                var borrowerDays = v.AutoSuggestedDepartmentCode == "PQLN" ? v.PqlnDays : v.PkhDays;
                var verifyWaitDays = v.PdvDays > 0 ? v.PdvDays : (WorkflowConstants.IsSalesDept(v.AutoSuggestedDepartmentCode) ? v.PqlnDays : 0);

                detailRows.Add(
                [
                    $"{p.Month:D2}/{p.Year}",
                    v.DocumentNumber,
                    v.CustomerName,
                    v.ItemName,
                    v.WorkflowType,
                    v.IssueDate.ToString("dd/MM/yyyy"),
                    v.SlaDueDate.ToString("dd/MM/yyyy"),
                    v.ActualCompletedDate?.ToString("dd/MM/yyyy") ?? "Chưa trả xong",
                    borrowerDays.ToString(),
                    verifyWaitDays.ToString(),
                    v.DelayWorkingDays.ToString(),
                    penalty.ToString("0.####"),
                    v.FinalDepartmentCode,
                    v.IsCarriedOver ? "Có" : "Không",
                    v.IsExcluded ? "Có" : "Không",
                    v.AdminNote ?? ""
                ]);
            }
        }

        return SimpleXlsxExporter.BuildWorkbook(
        [
            ("TongHopKPI", summaryRows),
            ("ChiTietChungTuNo", detailRows)
        ]);
    }

    private KpiPeriodDetailDto BuildPeriodDto(KpiPeriod p, KpiConfig cfg)
    {
        var scores = ComputeDepartmentScores(p, cfg);
        var items = p.Violations
            .OrderBy(x => x.IsExcluded)
            .ThenBy(x => x.SlaDueDate)
            .ThenBy(x => x.DocumentNumber)
            .Select(v => new KpiViolationItemDto(
                v.Id, v.DebtDocumentId, v.DebtItemId, v.DocumentNumber, v.CustomerName, v.ItemName,
                v.WorkflowType, v.IssueDate, v.SlaDueDate, v.ActualCompletedDate,
                v.PkhDays, v.PqlnDays, v.PdvDays, v.DelayWorkingDays,
                v.IsExcluded ? 0m : CalculateSingleItemDelayPenalty(v.DelayWorkingDays, cfg),
                v.IsCarriedOver, v.AutoSuggestedDepartmentCode, v.OverrideDepartmentCode,
                v.FinalDepartmentCode, v.IsExcluded, v.AdminNote))
            .ToList();

        return new KpiPeriodDetailDto(
            p.Id, p.Year, p.Month, p.WorkingDaysInMonth, p.CutoffDate,
            p.Status, p.CalculatedAt, p.FinalizedAt, p.Notes, scores, items);
    }

    private static List<DepartmentKpiScoreDto> ComputeDepartmentScores(KpiPeriod p, KpiConfig cfg)
    {
        var active = p.Violations.Where(v => !v.IsExcluded).ToList();

        return GetEffectiveDepartments(p.Violations).Select(dept =>
        {
            var deptItems = active.Where(v => v.FinalDepartmentCode == dept.Code).ToList();

            var distinctLateDays = deptItems
                .Where(v => !v.IsCarriedOver)
                .Select(v => v.SlaDueDate)
                .Distinct()
                .Count();

            var countPenalty = p.WorkingDaysInMonth <= 0
                ? 0m
                : Math.Round(cfg.MonthlyMaxCountPenalty * distinctLateDays / p.WorkingDaysInMonth, 4, MidpointRounding.AwayFromZero);

            var totalDelayDays = deptItems.Sum(v => v.DelayWorkingDays);
            var delayPenalty = Math.Round(
                deptItems.Sum(v => CalculateSingleItemDelayPenalty(v.DelayWorkingDays, cfg)),
                4,
                MidpointRounding.AwayFromZero);

            return new DepartmentKpiScoreDto(
                dept.Code,
                dept.Name,
                distinctLateDays,
                p.WorkingDaysInMonth,
                countPenalty,
                deptItems.Count,
                totalDelayDays,
                delayPenalty,
                countPenalty + delayPenalty);
        }).ToList();
    }

    private static decimal CalculateSingleItemDelayPenalty(int delayWorkingDays, KpiConfig cfg)
    {
        if (delayWorkingDays <= 0 || cfg.DelayCycleDays <= 0) return 0m;

        var ratio = (decimal)delayWorkingDays / cfg.DelayCycleDays;
        return cfg.DelayRoundingMode switch
        {
            "Floor" => Math.Floor(ratio) * cfg.DelayPenaltyRate,
            "Ceiling" => Math.Ceiling(ratio) * cfg.DelayPenaltyRate,
            _ => ratio * cfg.DelayPenaltyRate
        };
    }

    private static KpiConfigDto MapConfig(KpiConfig c) => new(
        c.Id, c.EffectiveFrom, c.MonthlyMaxCountPenalty, c.DelayPenaltyRate, c.DelayCycleDays,
        c.DelayRoundingMode, c.CompletionMilestoneType, c.SlaPkhDays, c.SlaPqlnDays, c.SlaPdvDays,
        c.DirectPdvTotalSlaDays, c.UpdatedAt);
}