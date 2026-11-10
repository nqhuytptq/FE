using DebtManagement.Application.DTOs;

namespace DebtManagement.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string email, string password);
}

public interface IUserService
{
    Task<IReadOnlyList<AppUserDto>> GetAllAsync();
    Task<IReadOnlyList<AppUserDto>> GetActiveAsync();
    Task<AppUserDto?> GetByIdAsync(Guid id);
    Task<AppUserDto> CreateAsync(CreateUserRequest request);
    Task<AppUserDto> UpdateAsync(Guid id, UpdateUserRequest request);
}

