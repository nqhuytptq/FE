using DebtManagement.Application.Interfaces;
using DebtManagement.Shared.Constants;
using DebtManagement.Shared.DTOs;

namespace DebtManagement.Application.Services;

public class SupReportService : ISupReportService
{
    private readonly IDebtDocumentService _docService;

    public SupReportService(IDebtDocumentService docService)
    {
        _docService = docService;
    }

    public async Task<SupOriginatedDebtReportDto> GetPkhSupReportAsync(Guid currentUserId)
    {
        var docs = await _docService.GetAllAsync(null, null, null, currentUserId);
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var onTime = new List<SupDebtItemReportRow>();
        var overdueUnpaid = new List<SupDebtItemReportRow>();
        var overduePaid = new List<SupDebtItemReportRow>();

        foreach (var d in docs.Where(x => x.TotalDebtItems > 0))
        {
            var detail = await _docService.GetByIdAsync(d.Id, currentUserId);
            if (detail == null) continue;

            var isDirectPdv = detail.WorkflowType == WorkflowConstants.WorkflowDirectPdv;
            var owedToDept = isDirectPdv ? WorkflowConstants.DeptPdv : WorkflowConstants.DeptPqln;
            var owedToHandler = isDirectPdv
                ? (detail.PdvReceiverName ?? "Chờ PDV tiếp nhận")
                : (detail.PqlnReceiverName ?? "Chờ PQLN tiếp nhận");

            var salesItems = detail.DebtItems
                .Where(i => WorkflowConstants.IsSalesDept(i.SourceDepartmentCode ?? detail.OriginDepartmentCode ?? WorkflowConstants.DeptPkh));

            foreach (var item in salesItems)
            {
                var srcCode = item.SourceDepartmentCode ?? detail.OriginDepartmentCode ?? WorkflowConstants.DeptPkh;
                var isVerified = isDirectPdv ? item.IsVerifiedByPdv : item.IsVerifiedByPqln;
                var verifiedAt = isDirectPdv ? item.PdvVerifiedAt : item.PqlnVerifiedAt;

                var row = BuildOriginatedRow(
                    detail,
                    item,
                    srcCode,
                    detail.CreatedByUserName ?? "—",
                    owedToDept,
                    owedToHandler,
                    isVerified,
                    verifiedAt,
                    todayVn);

                ClassifyRow(row, onTime, overdueUnpaid, overduePaid);
            }
        }

        var reportDept = docs.Select(x => x.OriginDepartmentCode).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                         ?? WorkflowConstants.DeptPkh;
        return BuildOriginatedReport(reportDept, onTime, overdueUnpaid, overduePaid);
    }

    public async Task<SupPqlnFullReportDto> GetPqlnSupReportAsync(Guid currentUserId)
    {
        var docs = await _docService.GetAllAsync(null, null, null, currentUserId);
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var receivedDocs = new List<PqlnReceivedDocumentReportRow>();
        var onTimeOriginated = new List<SupDebtItemReportRow>();
        var overdueUnpaidOriginated = new List<SupDebtItemReportRow>();
        var overduePaidOriginated = new List<SupDebtItemReportRow>();

        foreach (var d in docs)
        {
            var isStandardWorkflow = d.WorkflowType != WorkflowConstants.WorkflowDirectPdv
                                     && d.Status != WorkflowConstants.StatusNames.PkhPendingSup;

            if (!isStandardWorkflow && d.TotalDebtItems == 0)
                continue;

            var detail = await _docService.GetByIdAsync(d.Id, currentUserId);
            if (detail == null) continue;

            if (isStandardWorkflow)
            {
                var salesItems = detail.DebtItems
                    .Where(i => WorkflowConstants.IsSalesDept(i.SourceDepartmentCode ?? detail.OriginDepartmentCode ?? WorkflowConstants.DeptPkh))
                    .ToList();

                if (salesItems.Count > 0)
                {
                    var itemRows = salesItems
                        .Select(item =>
                        {
                            var srcCode = item.SourceDepartmentCode ?? detail.OriginDepartmentCode ?? WorkflowConstants.DeptPkh;
                            return BuildOriginatedRow(
                                detail,
                                item,
                                srcCode,
                                detail.CreatedByUserName ?? "—",
                                WorkflowConstants.DeptPqln,
                                detail.PqlnReceiverName ?? "Chờ PQLN tiếp nhận",
                                item.IsVerifiedByPqln,
                                item.PqlnVerifiedAt,
                                todayVn);
                        })
                        .ToList();

                    var paidCount = itemRows.Count(x => x.IsPaid);
                    var unpaidCount = itemRows.Count - paidCount;
                    var isDocOverdue = DateOnly.TryParse(detail.DueDate, out var due)
                                       && unpaidCount > 0
                                       && todayVn > due;

                    receivedDocs.Add(new PqlnReceivedDocumentReportRow(
                        detail.Id,
                        detail.DocumentNumber,
                        detail.CustomerName,
                        detail.CustomerCif,
                        detail.CustomerType,
                        detail.Status,
                        detail.DueDate,
                        isDocOverdue,
                        detail.CreatedByUserName ?? "—",
                        detail.PqlnReceiverName ?? "Chờ tiếp nhận",
                        itemRows.Count,
                        paidCount,
                        unpaidCount,
                        itemRows));
                }
            }

            var pqlnItems = detail.DebtItems
                .Where(i => i.SourceDepartmentCode == WorkflowConstants.DeptPqln);

            foreach (var item in pqlnItems)
            {
                var row = BuildOriginatedRow(
                    detail,
                    item,
                    WorkflowConstants.DeptPqln,
                    detail.PqlnReceiverName ?? "Chưa phân công PQLN",
                    WorkflowConstants.DeptPdv,
                    detail.PdvReceiverName ?? "Chờ PDV tiếp nhận",
                    item.IsVerifiedByPdv,
                    item.PdvVerifiedAt,
                    todayVn);

                ClassifyRow(row, onTimeOriginated, overdueUnpaidOriginated, overduePaidOriginated);
            }
        }

        var orderedReceivedDocs = receivedDocs
            .OrderByDescending(x => x.UnpaidPkhItems > 0)
            .ThenByDescending(x => x.IsOverdue)
            .ThenBy(x => x.DueDate)
            .ToList();

        var receivedReport = new PqlnReceivedReportDto(
            orderedReceivedDocs.Count,
            orderedReceivedDocs.Sum(x => x.TotalPkhItems),
            orderedReceivedDocs.Sum(x => x.PaidPkhItems),
            orderedReceivedDocs.Sum(x => x.UnpaidPkhItems),
            orderedReceivedDocs);

        var originatedReport = BuildOriginatedReport(
            WorkflowConstants.DeptPqln,
            onTimeOriginated,
            overdueUnpaidOriginated,
            overduePaidOriginated);

        return new SupPqlnFullReportDto(receivedReport, originatedReport);
    }

    public async Task<SupPdvFullReportDto> GetPdvSupReportAsync(Guid currentUserId)
    {
        var docs = await _docService.GetAllAsync(null, null, null, currentUserId);
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var pdvDocs = docs.Where(d => d.Status is
            WorkflowConstants.StatusNames.PdvPendingReceive or
            WorkflowConstants.StatusNames.PdvReceived or
            WorkflowConstants.StatusNames.PdvEntryPosted or
            WorkflowConstants.StatusNames.PdvEntryApproved or
            WorkflowConstants.StatusNames.Completed).ToList();

        var summaryRows = new List<PdvDocumentSummaryRow>();
        var allSalesRows = new List<SupDebtItemReportRow>();
        var allPqlnRows = new List<SupDebtItemReportRow>();

        foreach (var d in pdvDocs)
        {
            var detail = await _docService.GetByIdAsync(d.Id, currentUserId);
            if (detail == null) continue;

            var isDirectPdv = detail.WorkflowType == WorkflowConstants.WorkflowDirectPdv;
            var pdvReceiver = detail.PdvReceiverName ?? "Chờ PDV tiếp nhận";

            // 1. Chứng từ nợ của Khối bán hàng (KHDN, KHBL, PGD LLQ, PGD Cửa Đông)
            var salesItemRows = detail.DebtItems
                .Where(i => WorkflowConstants.IsSalesDept(i.SourceDepartmentCode ?? detail.OriginDepartmentCode ?? WorkflowConstants.DeptPkh))
                .Select(item =>
                {
                    var srcCode = item.SourceDepartmentCode ?? detail.OriginDepartmentCode ?? WorkflowConstants.DeptPkh;
                    var isVerified = isDirectPdv ? item.IsVerifiedByPdv : item.IsVerifiedByPqln;
                    var verifiedAt = isDirectPdv ? item.PdvVerifiedAt : item.PqlnVerifiedAt;
                    var targetDept = isDirectPdv ? WorkflowConstants.DeptPdv : WorkflowConstants.DeptPqln;
                    var targetHandler = isDirectPdv
                        ? pdvReceiver
                        : (detail.PqlnReceiverName ?? "Chờ PQLN tiếp nhận");

                    return BuildOriginatedRow(
                        detail,
                        item,
                        srcCode,
                        detail.CreatedByUserName ?? "—",
                        targetDept,
                        targetHandler,
                        isVerified,
                        verifiedAt,
                        todayVn);
                })
                .ToList();

            // 2. Chứng từ nợ của PQLN
            var pqlnItemRows = detail.DebtItems
                .Where(i => i.SourceDepartmentCode == WorkflowConstants.DeptPqln)
                .Select(item => BuildOriginatedRow(
                    detail,
                    item,
                    WorkflowConstants.DeptPqln,
                    detail.PqlnReceiverName ?? "Chưa phân công PQLN",
                    WorkflowConstants.DeptPdv,
                    pdvReceiver,
                    item.IsVerifiedByPdv,
                    item.PdvVerifiedAt,
                    todayVn))
                .ToList();

            allSalesRows.AddRange(salesItemRows);
            allPqlnRows.AddRange(pqlnItemRows);

            var salesPaid = salesItemRows.Count(x => x.IsPaid);
            var salesUnpaid = salesItemRows.Count - salesPaid;
            var pqlnPaid = pqlnItemRows.Count(x => x.IsPaid);
            var pqlnUnpaid = pqlnItemRows.Count - pqlnPaid;
            var totalUnpaid = salesUnpaid + pqlnUnpaid;

            var isDocOverdue = false;
            var overdueDays = 0;
            if (DateOnly.TryParse(detail.DueDate, out var dueDate) && totalUnpaid > 0 && todayVn > dueDate)
            {
                isDocOverdue = true;
                overdueDays = todayVn.DayNumber - dueDate.DayNumber;
            }

            var creatorDisplay = !string.IsNullOrWhiteSpace(detail.OriginDepartmentCode)
                ? $"{detail.CreatedByUserName ?? "—"} ({detail.OriginDepartmentCode})"
                : (detail.CreatedByUserName ?? "—");

            summaryRows.Add(new PdvDocumentSummaryRow(
                detail.Id,
                detail.DocumentNumber,
                detail.CustomerName,
                detail.CustomerCif,
                detail.CustomerType,
                detail.WorkflowType,
                detail.Status,
                detail.DueDate,
                isDocOverdue,
                overdueDays,
                creatorDisplay,
                isDirectPdv ? "— (Chuyển thẳng)" : (detail.PqlnReceiverName ?? "—"),
                pdvReceiver,
                salesItemRows.Count,
                salesPaid,
                salesUnpaid,
                pqlnItemRows.Count,
                pqlnPaid,
                pqlnUnpaid));
        }

        var orderedSummaries = summaryRows
            .OrderByDescending(x => (x.PkhUnpaidItems + x.PqlnUnpaidItems) > 0)
            .ThenByDescending(x => x.IsOverdue)
            .ThenByDescending(x => x.OverdueDays)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.DocumentNumber)
            .ToList();

        var orderedSales = allSalesRows
            .OrderBy(x => x.IsPaid)
            .ThenByDescending(x => x.IsOverdue)
            .ThenByDescending(x => x.OverdueDays)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.DocumentNumber)
            .ToList();

        var orderedPqln = allPqlnRows
            .OrderBy(x => x.IsPaid)
            .ThenByDescending(x => x.IsOverdue)
            .ThenByDescending(x => x.OverdueDays)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.DocumentNumber)
            .ToList();

        var docsWithUnpaid = orderedSummaries.Count(x => (x.PkhUnpaidItems + x.PqlnUnpaidItems) > 0);

        return new SupPdvFullReportDto(
            orderedSummaries.Count,
            docsWithUnpaid,
            orderedSummaries.Count - docsWithUnpaid,
            orderedSales.Count,
            orderedSales.Count(x => x.IsPaid),
            orderedSales.Count(x => !x.IsPaid),
            orderedPqln.Count,
            orderedPqln.Count(x => x.IsPaid),
            orderedPqln.Count(x => !x.IsPaid),
            orderedSummaries,
            orderedSales,
            orderedPqln);
    }

    private static SupDebtItemReportRow BuildOriginatedRow(
        DebtDocumentDetailDto detail,
        DebtItemDto item,
        string sourceDept,
        string createdByName,
        string owedToDept,
        string owedToHandler,
        bool isVerified,
        DateTime? verifiedAt,
        DateOnly todayVn)
    {
        var isPaid = item.IsSupplemented && isVerified;
        var hasDue = DateOnly.TryParse(detail.DueDate, out var dueDate);

        bool isOverdue = false;
        int overdueDays = 0;

        if (hasDue)
        {
            if (!isPaid)
            {
                if (todayVn > dueDate)
                {
                    isOverdue = true;
                    overdueDays = todayVn.DayNumber - dueDate.DayNumber;
                }
            }
            else
            {
                var completedUtc = verifiedAt ?? item.SupplementedAt ?? DateTime.UtcNow;
                var completedDateVn = DateOnly.FromDateTime(completedUtc.AddHours(7));
                if (completedDateVn > dueDate)
                {
                    isOverdue = true;
                    overdueDays = completedDateVn.DayNumber - dueDate.DayNumber;
                }
            }
        }

        return new SupDebtItemReportRow(
            detail.Id,
            detail.DocumentNumber,
            detail.CustomerName,
            detail.CustomerCif,
            detail.CustomerType,
            detail.WorkflowType,
            detail.Status,
            detail.DueDate,
            item.Id,
            item.ItemName,
            sourceDept,
            createdByName,
            owedToDept,
            owedToHandler,
            item.IsSupplemented,
            item.SupplementedAt,
            isVerified,
            verifiedAt,
            isPaid,
            isOverdue,
            overdueDays);
    }

    private static void ClassifyRow(
        SupDebtItemReportRow row,
        List<SupDebtItemReportRow> onTime,
        List<SupDebtItemReportRow> overdueUnpaid,
        List<SupDebtItemReportRow> overduePaid)
    {
        if (!row.IsOverdue)
            onTime.Add(row);
        else if (!row.IsPaid)
            overdueUnpaid.Add(row);
        else
            overduePaid.Add(row);
    }

    private static SupOriginatedDebtReportDto BuildOriginatedReport(
        string deptCode,
        List<SupDebtItemReportRow> onTime,
        List<SupDebtItemReportRow> overdueUnpaid,
        List<SupDebtItemReportRow> overduePaid)
    {
        var orderedOnTime = onTime.OrderBy(x => x.DueDate).ThenBy(x => x.DocumentNumber).ToList();
        var orderedUnpaid = overdueUnpaid.OrderByDescending(x => x.OverdueDays).ThenBy(x => x.DocumentNumber).ToList();
        var orderedPaid = overduePaid.OrderByDescending(x => x.OverdueDays).ThenBy(x => x.DocumentNumber).ToList();

        return new SupOriginatedDebtReportDto(
            deptCode,
            orderedOnTime.Count + orderedUnpaid.Count + orderedPaid.Count,
            orderedOnTime.Count,
            orderedUnpaid.Count + orderedPaid.Count,
            orderedUnpaid.Count,
            orderedPaid.Count,
            orderedOnTime,
            orderedUnpaid,
            orderedPaid);
    }
}