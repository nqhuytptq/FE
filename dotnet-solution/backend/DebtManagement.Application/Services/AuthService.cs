using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using DebtManagement.Application.DTOs;
using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


namespace DebtManagement.Application.Services;

public class AuthService(IUserRepository userRepo, IConfiguration config) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var user = await userRepo.GetByEmailAsync(email.Trim().ToLower());
        if (user == null || !user.IsActive) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

        var token = GenerateJwt(user);
        return new LoginResponse(token, MapUser(user));
    }

    private string GenerateJwt(AppUser user)
    {
        var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("name", user.Name),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static AppUserDto MapUser(AppUser u) => new(
        u.Id, u.Name, u.Email, u.Role,
        u.DepartmentId, u.Department?.Name, u.Department?.Code,
        u.IsActive, u.CreatedAt);
}

