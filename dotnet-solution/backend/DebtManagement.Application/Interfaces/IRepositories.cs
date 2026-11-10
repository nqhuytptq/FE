using DebtManagement.Domain.Entities;

namespace DebtManagement.Application.Interfaces;

public interface IDebtDocumentRepository
{
    Task<DebtDocument?> GetByIdAsync(Guid id);
    Task<DebtDocument?> GetDetailByIdAsync(Guid id);
    Task<IReadOnlyList<DebtDocument>> GetAllAsync(
        string? status = null, Guid? departmentId = null, string? search = null);
    Task<IReadOnlyList<CirculationHistory>> GetHistoryAsync(Guid documentId);
    Task<IReadOnlyList<DocumentEditLog>> GetEditLogsAsync(Guid documentId);
    Task<IReadOnlyList<DebtItem>> GetDebtItemsAsync(Guid documentId);
    Task AddAsync(DebtDocument document);
    Task UpdateAsync(DebtDocument document);
    Task DeleteAsync(Guid id);
    Task AddCirculationEntryAsync(CirculationHistory entry);
    Task AddEditLogAsync(DocumentEditLog log);
    Task AddDebtItemsAsync(IEnumerable<DebtItem> items);
    Task RemoveDebtItemsAsync(Guid documentId);
    Task UpdateDebtItemAsync(DebtItem item);
}

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(Guid id);
    Task<Department?> GetByCodeAsync(string code);
}

public interface IUserRepository
{
    Task<IReadOnlyList<AppUser>> GetAllAsync();
    Task<IReadOnlyList<AppUser>> GetActiveAsync();
    Task<IReadOnlyList<AppUser>> GetByDepartmentAsync(Guid departmentId);
    Task<AppUser?> GetByIdAsync(Guid id);
    Task<AppUser?> GetByEmailAsync(string email);
    Task AddAsync(AppUser user);
    Task UpdateAsync(AppUser user);
}

