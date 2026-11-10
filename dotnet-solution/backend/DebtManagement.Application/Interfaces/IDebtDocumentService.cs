using DebtManagement.Application.DTOs;

namespace DebtManagement.Application.Interfaces;

public interface IDebtDocumentService
{
    Task<IReadOnlyList<DebtDocumentDto>> GetAllAsync(string? status, Guid? departmentId, string? search, Guid currentUserId);
    Task<DebtDocumentDetailDto?> GetByIdAsync(Guid id, Guid currentUserId);

    Task<DebtDocumentDto> CreateAsync(CreateDebtDocumentRequest request, Guid userId);
    Task<DebtDocumentDto> UpdateAsync(Guid id, UpdateDebtDocumentRequest request, Guid userId);
    Task PkhSupApproveAsync(Guid id, string? notes, Guid supUserId);

    Task PqlnTakeOwnershipAsync(Guid id, Guid staffUserId);
    Task PqlnSubmitSupAsync(Guid id, string? notes, Guid staffUserId);
    Task PqlnSupApproveAsync(Guid id, string? notes, Guid supUserId);

    Task PdvTakeOwnershipAsync(Guid id, Guid staffUserId);
    Task PdvPostEntryAsync(Guid id, string? notes, Guid staffUserId);
    Task PdvSupApproveAsync(Guid id, string? notes, Guid supUserId);

    Task SupplementItemsAsync(Guid id, SupplementItemsRequest request, Guid userId);
    Task AddDebtItemsAsync(Guid id, AddDebtItemsRequest request, Guid userId);

    Task ReturnAsync(Guid id, Guid toDepartmentId, string? notes, Guid userId);
    Task DeleteAsync(Guid id);
}
