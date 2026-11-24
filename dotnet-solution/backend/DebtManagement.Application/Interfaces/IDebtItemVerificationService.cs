using DebtManagement.Shared.DTOs;

namespace DebtManagement.Application.Interfaces;

public interface IDebtItemVerificationService
{
    Task AddDebtItemsAsync(Guid id, AddDebtItemsRequest r, Guid u);
    Task UpdateDebtItemAsync(Guid documentId, Guid itemId, UpdateDebtItemRequest request, Guid userId);
    Task DeleteDebtItemAsync(Guid documentId, Guid itemId, Guid userId);
    Task SupplementItemsAsync(Guid id, SupplementItemsRequest r, Guid u);
    Task VerifySupplementItemsAsync(Guid id, VerifySupplementItemsRequest r, Guid u);
    Task VerifyPqlnSupplementItemsAsync(Guid id, VerifyPqlnSupplementItemsRequest r, Guid u);
    Task RejectSupplementItemAsync(Guid documentId, Guid itemId, RejectSupplementItemRequest request, Guid userId);
}