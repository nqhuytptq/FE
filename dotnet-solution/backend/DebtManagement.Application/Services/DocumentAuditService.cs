using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;

namespace DebtManagement.Application.Services;

public class DocumentAuditService(IDebtDocumentRepository docs) : IDocumentAuditService
{
    public Task AddHistoryAsync(
        Guid documentId,
        Guid? fromDepartmentId,
        Guid? toDepartmentId,
        CirculationAction action,
        DocumentStatus statusAfter,
        Guid performedByUserId,
        string note)
        => docs.AddCirculationEntryAsync(new CirculationHistory
        {
            DebtDocumentId = documentId,
            FromDepartmentId = fromDepartmentId,
            ToDepartmentId = toDepartmentId,
            Action = action,
            StatusAfter = statusAfter,
            PerformedByUserId = performedByUserId,
            Notes = note
        });
}