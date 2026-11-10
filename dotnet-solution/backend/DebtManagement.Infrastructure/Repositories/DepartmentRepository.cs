using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using DebtManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DebtManagement.Infrastructure.Repositories;

public class DepartmentRepository(AppDbContext db) : IDepartmentRepository
{
    public async Task<IReadOnlyList<Department>> GetAllAsync()
        => await db.Departments.OrderBy(d => d.SortOrder).ToListAsync();

    public async Task<Department?> GetByIdAsync(Guid id)
        => await db.Departments.FindAsync(id);

    public async Task<Department?> GetByCodeAsync(string code)
        => await db.Departments.FirstOrDefaultAsync(d => d.Code == code);
}

