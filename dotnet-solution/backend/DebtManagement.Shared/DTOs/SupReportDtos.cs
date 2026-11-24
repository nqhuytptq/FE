namespace DebtManagement.Shared.DTOs;

/// <summary>
/// Dòng chi tiết chứng từ nợ dùng cho báo cáo khởi tạo của PKH và PQLN, hoặc chi tiết nợ tại PDV
/// </summary>
public record SupDebtItemReportRow(
    Guid DocumentId,
    string DocumentNumber,
    string CustomerName,
    string? CustomerCif,
    string CustomerType,
    string WorkflowType,
    string DocumentStatus,
    string DueDate,
    Guid DebtItemId,
    string ItemName,
    string SourceDepartmentCode,
    string CreatedByName,
    string OwedToDepartment,
    string OwedToHandlerName,
    bool IsSupplemented,
    DateTime? SupplementedAt,
    bool IsVerifiedByTarget,
    DateTime? VerifiedAt,
    bool IsPaid,
    bool IsOverdue,
    int OverdueDays
);

/// <summary>
/// Báo cáo tổng hợp theo nhóm Đúng hạn / Quá hạn (Đã trả & Chưa trả) cho PKH và PQLN (mục Khởi tạo)
/// </summary>
public record SupOriginatedDebtReportDto(
    string DepartmentCode,
    int TotalItems,
    int OnTimeCount,
    int OverdueTotalCount,
    int OverdueUnpaidCount,
    int OverduePaidCount,
    List<SupDebtItemReportRow> OnTimeItems,
    List<SupDebtItemReportRow> OverdueUnpaidItems,
    List<SupDebtItemReportRow> OverduePaidItems
);

/// <summary>
/// Chi tiết 1 văn bản PKH chuyển sang PQLN trong mục Tiếp nhận của SUP PQLN
/// </summary>
public record PqlnReceivedDocumentReportRow(
    Guid DocumentId,
    string DocumentNumber,
    string CustomerName,
    string? CustomerCif,
    string CustomerType,
    string DocumentStatus,
    string DueDate,
    bool IsOverdue,
    string PkhCreatorName,
    string PqlnReceiverName,
    int TotalPkhItems,
    int PaidPkhItems,
    int UnpaidPkhItems,
    List<SupDebtItemReportRow> Items
);

/// <summary>
/// Báo cáo mục Tiếp nhận (nhận từ PKH) dành riêng cho SUP PQLN
/// </summary>
public record PqlnReceivedReportDto(
    int TotalDocuments,
    int TotalPkhDebtItems,
    int PaidItemsCount,
    int UnpaidItemsCount,
    List<PqlnReceivedDocumentReportRow> Documents
);

/// <summary>
/// Gói dữ liệu báo cáo tổng hợp cho SUP PQLN (gồm 2 phần: Tiếp nhận & Khởi tạo)
/// </summary>
public record SupPqlnFullReportDto(
    PqlnReceivedReportDto ReceivedFromPkh,
    SupOriginatedDebtReportDto OriginatedToPdv
);

/// <summary>
/// Chi tiết tổng hợp 1 văn bản tại PDV (gồm tình hình nợ của PKH và nợ của PQLN)
/// </summary>
public record PdvDocumentSummaryRow(
    Guid DocumentId,
    string DocumentNumber,
    string CustomerName,
    string? CustomerCif,
    string CustomerType,
    string WorkflowType,
    string DocumentStatus,
    string DueDate,
    bool IsOverdue,
    int OverdueDays,
    string PkhCreatorName,
    string PqlnHandlerName,
    string PdvReceiverName,
    int PkhTotalItems,
    int PkhPaidItems,
    int PkhUnpaidItems,
    int PqlnTotalItems,
    int PqlnPaidItems,
    int PqlnUnpaidItems
);

/// <summary>
/// Gói dữ liệu báo cáo tổng hợp cho SUP PDV:
/// 1. Tổng hợp các văn bản
/// 2. Chi tiết nợ của PKH
/// 3. Chi tiết nợ của PQLN
/// </summary>
public record SupPdvFullReportDto(
    int TotalDocuments,
    int DocumentsWithUnpaidDebt,
    int DocumentsFullyPaidOrNoDebt,
    int PkhTotalItems,
    int PkhPaidItems,
    int PkhUnpaidItems,
    int PqlnTotalItems,
    int PqlnPaidItems,
    int PqlnUnpaidItems,
    List<PdvDocumentSummaryRow> Documents,
    List<SupDebtItemReportRow> PkhDebtItems,
    List<SupDebtItemReportRow> PqlnDebtItems
);