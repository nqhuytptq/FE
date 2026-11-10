using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using DebtManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DebtManagement.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<IReadOnlyList<AppUser>> GetAllAsync()
        => await db.AppUsers.Include(u => u.Department).OrderBy(u => u.Name).ToListAsync();

    public async Task<IReadOnlyList<AppUser>> GetActiveAsync()
        => await db.AppUsers.Include(u => u.Department)
            .Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();

    public async Task<IReadOnlyList<AppUser>> GetByDepartmentAsync(Guid departmentId)
        => await db.AppUsers.Include(u => u.Department)
            .Where(u => u.DepartmentId == departmentId && u.IsActive)
            .OrderBy(u => u.Name).ToListAsync();

    public async Task<AppUser?> GetByIdAsync(Guid id)
        => await db.AppUsers.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<AppUser?> GetByEmailAsync(string email)
        => await db.AppUsers.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(AppUser user)
    {
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppUser user)
    {
        db.AppUsers.Update(user);
        await db.SaveChangesAsync();
    }
}

