using DebtManagement.Domain.Entities;

namespace DebtManagement.Application.Interfaces;

public interface IDocumentAuditService
{
    Task AddHistoryAsync(
        Guid documentId,
        Guid? fromDepartmentId,
        Guid? toDepartmentId,
        CirculationAction action,
        DocumentStatus statusAfter,
        Guid performedByUserId,
        string note);
}