using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using DebtManagement.Shared.Constants;
using DebtManagement.Shared.DTOs;

namespace DebtManagement.Application.Services;

public class KpiCalculationService(
    IKpiRepository kpiRepo,
    IWorkingCalendarService calendar,
    IUserRepository users) : IKpiCalculationService
{
    private static readonly (string Code, string Name)[] TrackedDepartments =
    [
        (WorkflowConstants.DeptPkh, "Phòng Khách hàng"),
        (WorkflowConstants.DeptPqln, "Phòng Quản lý nợ"),
        (WorkflowConstants.DeptPdv, "Phòng Dịch vụ khách hàng")
    ];

    private async Task EnsureAdminAsync(Guid userId)
    {
        var user = await users.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Tài khoản không tồn tại");
        if (user.Role != WorkflowConstants.RoleAdmin)
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

        // Lưu lại chỉnh sửa thủ công trước đó (nếu đang Draft) để không bị mất khi bấm Tính lại
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
            var isDirectPdv = doc.WorkflowType == WorkflowConstants.WorkflowDirectPdv;
            var docTotalSla = isDirectPdv ? cfg.DirectPdvTotalSlaDays : totalStandardSla;
            var slaDueDate = calendar.AddWorkingDays(doc.IssueDate, docTotalSla, holidayMap);

            // Chưa đến hạn trong tháng này thì chuyển sang xét ở tháng sau
            if (slaDueDate > periodEnd) continue;

            // Trường hợp hồ sơ có DebtItems -> tính trên từng chứng từ; nếu không có DebtItem -> lấy 1 dòng đại diện cho hồ sơ
            if (doc.DebtItems.Count > 0)
            {
                foreach (var item in doc.DebtItems)
                {
                    var violation = EvaluateTarget(
                        periodId, doc, item, slaDueDate, isDirectPdv,
                        periodStart, periodEnd, prevPeriodEnd, cfg, holidayMap, preservedOverrides);
                    if (violation != null)
                        newViolations.Add(violation);
                }
            }
            else
            {
                var violation = EvaluateTarget(
                    periodId, doc, null, slaDueDate, isDirectPdv,
                    periodStart, periodEnd, prevPeriodEnd, cfg, holidayMap, preservedOverrides);
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

    private KpiViolationItem? EvaluateTarget(
        Guid periodId,
        DebtDocument doc,
        DebtItem? item,
        DateOnly slaDueDate,
        bool isDirectPdv,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly prevPeriodEnd,
        KpiConfig cfg,
        IReadOnlyDictionary<DateOnly, bool> holidayMap,
        Dictionary<(Guid, Guid?), (string? OverrideDept, bool IsExcluded, string? AdminNote)> preservedOverrides)
    {
        var actualCompletedDate = ResolveActualCompletedDate(doc, item, isDirectPdv, cfg.CompletionMilestoneType);

        // Nếu đã hoàn tất đúng/trước hạn SLA thì không vi phạm
        if (actualCompletedDate.HasValue && actualCompletedDate.Value <= slaDueDate)
            return null;

        // Nếu thuộc kỳ cũ và đã hoàn tất trước đầu tháng này thì kỳ này không tính nữa
        var isCarriedOver = slaDueDate < periodStart;
        if (isCarriedOver && actualCompletedDate.HasValue && actualCompletedDate.Value < periodStart)
            return null;

        var evalEnd = actualCompletedDate.HasValue && actualCompletedDate.Value <= periodEnd
            ? actualCompletedDate.Value
            : periodEnd;

        var delayDays = isCarriedOver
            ? calendar.CountWorkingDaysBetween(prevPeriodEnd, evalEnd, holidayMap)
            : calendar.CountWorkingDaysBetween(slaDueDate, evalEnd, holidayMap);

        // Nếu kỳ chuyển tiếp đã hết trễ thêm trong tháng mới thì bỏ qua
        if (isCarriedOver && delayDays <= 0)
            return null;

        var (pkhDays, pqlnDays, pdvDays, autoDept) = ComputeDepartmentHoldingAndAttribution(
            doc, item, isDirectPdv, evalEnd, cfg, holidayMap);

        var key = (doc.Id, item?.Id);
        preservedOverrides.TryGetValue(key, out var prev);

        var finalDept = string.IsNullOrWhiteSpace(prev.OverrideDept) ? autoDept : prev.OverrideDept!;

        return new KpiViolationItem
        {
            KpiPeriodId = periodId,
            DebtDocumentId = doc.Id,
            DebtItemId = item?.Id,
            DocumentNumber = doc.DocumentNumber,
            CustomerName = doc.CustomerName,
            ItemName = item?.ItemName ?? "(Hồ sơ không có chứng từ nợ lẻ)",
            WorkflowType = doc.WorkflowType,
            IssueDate = doc.IssueDate,
            SlaDueDate = slaDueDate,
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

    private static DateOnly? ResolveActualCompletedDate(
        DebtDocument doc,
        DebtItem? item,
        bool isDirectPdv,
        string milestoneType)
    {
        if (milestoneType == "EntryPostedAt")
        {
            if (item == null)
                return doc.EntryPostedAt.HasValue ? ToVnDate(doc.EntryPostedAt.Value) : null;

            // Với chế độ EntryPostedAt: lấy ngày sau cùng của đi bút toán và xác nhận chứng từ
            var itemDoneAt = GetItemFinalVerifiedAt(doc, item, isDirectPdv);
            if (!doc.EntryPostedAt.HasValue || !itemDoneAt.HasValue) return null;
            var maxDt = doc.EntryPostedAt.Value > itemDoneAt.Value ? doc.EntryPostedAt.Value : itemDoneAt.Value;
            return ToVnDate(maxDt);
        }

        // Mặc định: CompletedAt (hoặc PdvVerifiedAt/PqlnVerifiedAt sau khi hồ sơ hoàn tất)
        if (item == null)
            return doc.CompletedAt.HasValue ? ToVnDate(doc.CompletedAt.Value) : null;

        var verifiedAt = GetItemFinalVerifiedAt(doc, item, isDirectPdv);
        if (verifiedAt.HasValue)
        {
            if (doc.CompletedAt.HasValue && doc.CompletedAt.Value > verifiedAt.Value)
                return ToVnDate(verifiedAt.Value);
            return ToVnDate(verifiedAt.Value);
        }

        return doc.CompletedAt.HasValue ? ToVnDate(doc.CompletedAt.Value) : null;
    }

    private static DateTime? GetItemFinalVerifiedAt(DebtDocument doc, DebtItem item, bool isDirectPdv)
    {
        if (!item.IsSupplemented) return null;
        if (isDirectPdv) return item.PdvVerifiedAt;

        var srcCode = item.SourceDepartment?.Code ?? WorkflowConstants.DeptPkh;
        if (srcCode == WorkflowConstants.DeptPqln)
            return item.PdvVerifiedAt;

        // Chứng từ PKH đi qua PQLN: nếu đã được PDV verify thì lấy PdvVerifiedAt, ngược lại lấy PqlnVerifiedAt
        return item.PdvVerifiedAt ?? item.PqlnVerifiedAt;
    }

    private (int PkhDays, int PqlnDays, int PdvDays, string AutoDept) ComputeDepartmentHoldingAndAttribution(
        DebtDocument doc,
        DebtItem? item,
        bool isDirectPdv,
        DateOnly evalEnd,
        KpiConfig cfg,
        IReadOnlyDictionary<DateOnly, bool> holidayMap)
    {
        var issueDate = doc.IssueDate;
        var srcCode = item?.SourceDepartment?.Code ?? WorkflowConstants.DeptPkh;

        // 1. Thời gian PKH giữ
        DateOnly? pkhApproveDate = doc.SupPkhApprovedAt.HasValue ? ToVnDate(doc.SupPkhApprovedAt.Value) : null;
        DateOnly pkhEnd = pkhApproveDate ?? evalEnd;

        if (item != null && srcCode == WorkflowConstants.DeptPkh)
        {
            if (item.SupplementedAt.HasValue)
            {
                var suppDate = ToVnDate(item.SupplementedAt.Value);
                if (suppDate > pkhEnd) pkhEnd = suppDate;
            }
            else if (evalEnd > pkhEnd)
            {
                pkhEnd = evalEnd; // PKH vẫn chưa bổ sung chứng từ này
            }
        }
        var pkhDays = calendar.CountWorkingDaysBetween(issueDate, pkhEnd, holidayMap);

        // 2. Thời gian PQLN giữ
        int pqlnDays = 0;
        DateOnly? pqlnApproveDate = doc.SupPqlnApprovedAt.HasValue ? ToVnDate(doc.SupPqlnApprovedAt.Value) : null;

        if (!isDirectPdv && pkhApproveDate.HasValue)
        {
            var pqlnStage1End = pqlnApproveDate ?? evalEnd;
            pqlnDays += calendar.CountWorkingDaysBetween(pkhApproveDate.Value, pqlnStage1End, holidayMap);

            if (item != null && srcCode == WorkflowConstants.DeptPkh && item.SupplementedAt.HasValue)
            {
                var suppDate = ToVnDate(item.SupplementedAt.Value);
                var verifyWaitStart = suppDate > pkhApproveDate.Value ? suppDate : pkhApproveDate.Value;
                if (pqlnApproveDate.HasValue && verifyWaitStart > pqlnApproveDate.Value)
                {
                    var verifyEnd = item.PqlnVerifiedAt.HasValue ? ToVnDate(item.PqlnVerifiedAt.Value) : evalEnd;
                    pqlnDays += calendar.CountWorkingDaysBetween(verifyWaitStart, verifyEnd, holidayMap);
                }
            }
            else if (item != null && srcCode == WorkflowConstants.DeptPqln)
            {
                if (pqlnApproveDate.HasValue)
                {
                    var pqlnSuppEnd = item.SupplementedAt.HasValue ? ToVnDate(item.SupplementedAt.Value) : evalEnd;
                    if (pqlnSuppEnd > pqlnApproveDate.Value)
                        pqlnDays += calendar.CountWorkingDaysBetween(pqlnApproveDate.Value, pqlnSuppEnd, holidayMap);
                }
            }
        }

        // 3. Thời gian PDV giữ
        int pdvDays = 0;
        DateOnly? prevDeptDone = isDirectPdv ? pkhApproveDate : pqlnApproveDate;
        if (prevDeptDone.HasValue)
        {
            var docDoneDate = cfg.CompletionMilestoneType == "EntryPostedAt"
                ? (doc.EntryPostedAt.HasValue ? ToVnDate(doc.EntryPostedAt.Value) : (DateOnly?)null)
                : (doc.CompletedAt.HasValue ? ToVnDate(doc.CompletedAt.Value) : (DateOnly?)null);

            var entryApprovedDate = doc.SupPdvApprovedEntryAt.HasValue ? ToVnDate(doc.SupPdvApprovedEntryAt.Value) : (DateOnly?)null;

            if (item == null)
            {
                pdvDays = calendar.CountWorkingDaysBetween(prevDeptDone.Value, docDoneDate ?? evalEnd, holidayMap);
            }
            else
            {
                var needsPdvVerify = isDirectPdv || srcCode == WorkflowConstants.DeptPqln;
                if (needsPdvVerify)
                {
                    // Chặng 1: PDV xử lý từ khi nhận đến khi duyệt bút toán
                    var stage1End = entryApprovedDate ?? docDoneDate ?? evalEnd;
                    pdvDays += calendar.CountWorkingDaysBetween(prevDeptDone.Value, stage1End, holidayMap);

                    // Chặng 2: sau khi chứng từ được bổ sung -> PDV verify
                    if (item.SupplementedAt.HasValue)
                    {
                        var suppDate = ToVnDate(item.SupplementedAt.Value);
                        var waitStart = suppDate > prevDeptDone.Value ? suppDate : prevDeptDone.Value;
                        if (entryApprovedDate.HasValue && waitStart > entryApprovedDate.Value)
                        {
                            var verifyEnd = item.PdvVerifiedAt.HasValue ? ToVnDate(item.PdvVerifiedAt.Value) : (docDoneDate ?? evalEnd);
                            pdvDays += calendar.CountWorkingDaysBetween(waitStart, verifyEnd, holidayMap);
                        }
                    }
                }
                else
                {
                    // Chứng từ PKH trong luồng chuẩn: nếu PDV đã duyệt bút toán và đang chờ PKH/PQLN thì không tính vào PDV
                    var stageEnd = entryApprovedDate ?? docDoneDate ?? evalEnd;
                    pdvDays = calendar.CountWorkingDaysBetween(prevDeptDone.Value, stageEnd, holidayMap);
                }
            }
        }

        // 4. Gợi ý phòng chịu lỗi chính
        // Ưu tiên nếu chứng từ đang kẹt chưa bổ sung/xác nhận ở khâu cụ thể
        if (item != null && !item.IsSupplemented)
            return (pkhDays, pqlnDays, pdvDays, srcCode);

        if (item != null && item.IsSupplemented)
        {
            if (!isDirectPdv && srcCode == WorkflowConstants.DeptPkh && !item.IsVerifiedByPqln)
                return (pkhDays, pqlnDays, pdvDays, WorkflowConstants.DeptPqln);
            if ((isDirectPdv || srcCode == WorkflowConstants.DeptPqln) && !item.IsVerifiedByPdv)
                return (pkhDays, pqlnDays, pdvDays, WorkflowConstants.DeptPdv);
        }

        // So sánh mức vượt SLA của từng phòng
        var pkhOver = pkhDays - cfg.SlaPkhDays;
        var pqlnOver = isDirectPdv ? int.MinValue : (pqlnDays - cfg.SlaPqlnDays);
        var pdvOver = pdvDays - cfg.SlaPdvDays;

        var candidates = new List<(string Code, int Over)>
        {
            (WorkflowConstants.DeptPkh, pkhOver)
        };
        if (!isDirectPdv)
            candidates.Add((WorkflowConstants.DeptPqln, pqlnOver));
        candidates.Add((WorkflowConstants.DeptPdv, pdvOver));

        var best = candidates.OrderByDescending(x => x.Over).First();
        if (best.Over > 0)
            return (pkhDays, pqlnDays, pdvDays, best.Code);

        // Nếu tổng cộng dồn vượt SLA dù từng phòng sát nút: chọn phòng hiện tại hoặc phòng giữ lâu nhất
        var fallback = doc.Status.ToString() switch
        {
            WorkflowConstants.StatusNames.PkhPendingSup or WorkflowConstants.StatusNames.ReturnedToPkh => WorkflowConstants.DeptPkh,
            WorkflowConstants.StatusNames.PqlnPendingReceive or WorkflowConstants.StatusNames.PqlnReceived or WorkflowConstants.StatusNames.PqlnPendingSup or WorkflowConstants.StatusNames.ReturnedToPqln => WorkflowConstants.DeptPqln,
            _ => candidates.OrderByDescending(x => x.Over).First().Code
        };cd ..

        return (pkhDays, pqlnDays, pdvDays, fallback);
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

        var cumulative = TrackedDepartments.Select(dept =>
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
            new[] { "Mã phòng", "Tên phòng", "Số ngày phát sinh nợ (II.1)", "Tổng ngày làm việc", "Điểm trừ SL chứng từ (II.1)", "Số chứng từ trễ (II.2)", "Tổng ngày trễ", "Điểm trừ thời gian nợ (II.2)", "Tổng điểm trừ" }
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
            new[] { "Kỳ", "Số văn bản", "Khách hàng", "Chứng từ nợ", "Luồng", "Ngày phát sinh", "Hạn SLA", "Ngày hoàn tất", "PKH (ngày)", "PQLN (ngày)", "PDV (ngày)", "Số ngày trễ", "Điểm trừ II.2", "Phòng chịu lỗi", "Chuyển tiếp kỳ trước", "Loại trừ", "Ghi chú" }
        };

        foreach (var p in periods)
        {
            foreach (var v in p.Violations.OrderBy(x => x.IssueDate))
            {
                var penalty = v.IsExcluded ? 0m : CalculateSingleItemDelayPenalty(v.DelayWorkingDays, cfg);
                detailRows.Add(
                [
                    $"{p.Month:D2}/{p.Year}",
                    v.DocumentNumber,
                    v.CustomerName,
                    v.ItemName,
                    v.WorkflowType,
                    v.IssueDate.ToString("dd/MM/yyyy"),
                    v.SlaDueDate.ToString("dd/MM/yyyy"),
                    v.ActualCompletedDate?.ToString("dd/MM/yyyy") ?? "Chưa hoàn tất",
                    v.PkhDays.ToString(),
                    v.PqlnDays.ToString(),
                    v.PdvDays.ToString(),
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
            .ThenBy(x => x.IssueDate)
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

        return TrackedDepartments.Select(dept =>
        {
            var deptItems = active.Where(v => v.FinalDepartmentCode == dept.Code).ToList();
            var distinctLateDays = deptItems
                .Where(v => !v.IsCarriedOver)
                .Select(v => v.IssueDate)
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