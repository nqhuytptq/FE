using DebtManagement.Application.DTOs;
using DebtManagement.Application.Interfaces;

namespace DebtManagement.BlazorApp;

public sealed class BlazorSession(IAuthService authService)
{
    public AppUserDto? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public async Task<string?> LoginAsync(string email, string password)
    {
        try
        {
            var result = await authService.LoginAsync(email, password);
            CurrentUser = result?.User;
            return result == null ? "Email hoặc mật khẩu không đúng" : null;
        }
        catch (Exception ex)
        {
            CurrentUser = null;
            return $"Không thể đăng nhập: {ex.Message}";
        }
    }

    public void Logout() => CurrentUser = null;
}
