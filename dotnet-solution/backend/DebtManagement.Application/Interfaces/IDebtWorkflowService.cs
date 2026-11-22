namespace DebtManagement.Application.Interfaces;

public interface IDebtWorkflowService
{
    Task PkhSupApproveAsync(Guid id, string? n, Guid u);
    Task PqlnTakeOwnershipAsync(Guid id, Guid u);
    Task PqlnSubmitSupAsync(Guid id, string? n, Guid u);
    Task PqlnSupApproveAsync(Guid id, string? n, Guid u);
    Task PdvTakeOwnershipAsync(Guid id, Guid u);
    Task PdvPostEntryAsync(Guid id, string? n, Guid u);
    Task PdvSupApproveAsync(Guid id, string? n, Guid u);
    Task ReturnAsync(Guid id, Guid to, string? n, Guid u);
}