using System.Text.Json;
using DebtManagement.Shared.DTOs;
using DebtManagement.Application.Interfaces;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DebtManagement.BlazorApp;

public sealed class BlazorSession(IAuthService authService, ProtectedLocalStorage storage)
{
    private const string StorageKey = "vcb-current-user";
    public AppUserDto? CurrentUser { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;
    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        try
        {
            var stored = await storage.GetAsync<AppUserDto>(StorageKey);
            if (stored.Success) CurrentUser = stored.Value;
        }
        catch
        {
            CurrentUser = null;
        }
        finally
        {
            IsInitialized = true;
            OnChange?.Invoke();
        }
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        try
        {
            var result = await authService.LoginAsync(email, password);
            CurrentUser = result?.User;
            if (CurrentUser != null)
                await storage.SetAsync(StorageKey, CurrentUser);
            OnChange?.Invoke();
            return result == null ? "Email hoặc mật khẩu không đúng" : null;
        }
        catch (Exception ex)
        {
            CurrentUser = null;
            return $"Không thể đăng nhập: {ex.Message}";
        }
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        try { await storage.DeleteAsync(StorageKey); } catch { }
        OnChange?.Invoke();
    }
}

