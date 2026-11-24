using DebtManagement.Shared.DTOs;

namespace DebtManagement.Application.Interfaces;

public interface ISupReportService
{
    Task<SupOriginatedDebtReportDto> GetPkhSupReportAsync(Guid currentUserId);
    Task<SupPqlnFullReportDto> GetPqlnSupReportAsync(Guid currentUserId);
    Task<SupPdvFullReportDto> GetPdvSupReportAsync(Guid currentUserId);
}