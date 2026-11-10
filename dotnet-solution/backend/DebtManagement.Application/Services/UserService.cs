using BCrypt.Net;
using DebtManagement.Application.DTOs;
using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;


namespace DebtManagement.Application.Services;

public class UserService(IUserRepository userRepo) : IUserService
{
    private static AppUserDto Map(AppUser u) => AuthService.MapUser(u);

    public async Task<IReadOnlyList<AppUserDto>> GetAllAsync()
        => (await userRepo.GetAllAsync()).Select(Map).ToList();

    public async Task<IReadOnlyList<AppUserDto>> GetActiveAsync()
        => (await userRepo.GetActiveAsync()).Select(Map).ToList();

    public async Task<AppUserDto?> GetByIdAsync(Guid id)
    {
        var u = await userRepo.GetByIdAsync(id);
        return u == null ? null : Map(u);
    }

    public async Task<AppUserDto> CreateAsync(CreateUserRequest request)
    {
        var existing = await userRepo.GetByEmailAsync(request.Email.Trim().ToLower());
        if (existing != null) throw new InvalidOperationException("Email đã tồn tại");

        var user = new AppUser
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            DepartmentId = request.DepartmentId,
            IsActive = true,
        };
        await userRepo.AddAsync(user);
        return Map((await userRepo.GetByIdAsync(user.Id))!);
    }

    public async Task<AppUserDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await userRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Người dùng không tồn tại");
        user.Name = request.Name.Trim();
        user.Role = request.Role;
        user.DepartmentId = request.DepartmentId;
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await userRepo.UpdateAsync(user);
        return Map((await userRepo.GetByIdAsync(user.Id))!);
    }
}

