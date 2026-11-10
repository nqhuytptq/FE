using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using DebtManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DebtManagement.Infrastructure.Repositories;

public class DebtDocumentRepository(AppDbContext db) : IDebtDocumentRepository
{
    private static IQueryable<DebtDocument> Base(AppDbContext db) => db.DebtDocuments
        .Include(d => d.CurrentDepartment)
        .Include(d => d.CreatedByUser)
        .Include(d => d.PqlnReceiver)
        .Include(d => d.PdvReceiver)
        .Include(d => d.DebtItems).ThenInclude(i => i.SupplementedByUser)
        .Include(d => d.DebtItems).ThenInclude(i => i.SourceDepartment)
        .Include(d => d.DebtItems).ThenInclude(i => i.CreatedByUser);

    public async Task<IReadOnlyList<DebtDocument>> GetAllAsync(
        string? status = null, Guid? departmentId = null, string? search = null)
    {
        var q = Base(db).AsQueryable();
        if (!string.IsNullOrEmpty(status))
            q = q.Where(d => d.Status == Enum.Parse<DocumentStatus>(status, true));
        if (departmentId.HasValue)
            q = q.Where(d => d.CurrentDepartmentId == departmentId);
        if (!string.IsNullOrEmpty(search))
            q = q.Where(d => d.CustomerName.Contains(search) || d.DocumentNumber.Contains(search));
        return await q.OrderByDescending(d => d.CreatedAt).ToListAsync();
    }

    public async Task<DebtDocument?> GetByIdAsync(Guid id)
        => await Base(db).FirstOrDefaultAsync(d => d.Id == id);

    public async Task<DebtDocument?> GetDetailByIdAsync(Guid id)
        => await Base(db).FirstOrDefaultAsync(d => d.Id == id);

    public async Task<IReadOnlyList<CirculationHistory>> GetHistoryAsync(Guid documentId)
        => await db.CirculationHistory
            .Include(h => h.FromDepartment)
            .Include(h => h.ToDepartment)
            .Include(h => h.PerformedByUser)
            .Where(h => h.DebtDocumentId == documentId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<DocumentEditLog>> GetEditLogsAsync(Guid documentId)
        => await db.DocumentEditLogs
            .Include(l => l.EditedByUser)
            .Where(l => l.DebtDocumentId == documentId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<DebtItem>> GetDebtItemsAsync(Guid documentId)
        => await db.DebtItems
            .Include(i => i.SupplementedByUser)
            .Include(i => i.SourceDepartment)
            .Include(i => i.CreatedByUser)
            .Where(i => i.DebtDocumentId == documentId)
            .ToListAsync();

    public async Task AddAsync(DebtDocument document) { db.DebtDocuments.Add(document); await db.SaveChangesAsync(); }
    public async Task UpdateAsync(DebtDocument document) { document.UpdatedAt = DateTime.UtcNow; db.DebtDocuments.Update(document); await db.SaveChangesAsync(); }
    public async Task DeleteAsync(Guid id) { var d = await db.DebtDocuments.FindAsync(id); if (d != null) { db.DebtDocuments.Remove(d); await db.SaveChangesAsync(); } }
    public async Task AddCirculationEntryAsync(CirculationHistory e) { db.CirculationHistory.Add(e); await db.SaveChangesAsync(); }
    public async Task AddEditLogAsync(DocumentEditLog l) { db.DocumentEditLogs.Add(l); await db.SaveChangesAsync(); }
    public async Task AddDebtItemsAsync(IEnumerable<DebtItem> items) { db.DebtItems.AddRange(items); await db.SaveChangesAsync(); }
    public async Task RemoveDebtItemsAsync(Guid documentId) { var items = await db.DebtItems.Where(i => i.DebtDocumentId == documentId).ToListAsync(); if (items.Count > 0) { db.DebtItems.RemoveRange(items); await db.SaveChangesAsync(); } }
    public async Task UpdateDebtItemAsync(DebtItem item) { db.DebtItems.Update(item); await db.SaveChangesAsync(); }
}
