using DebtManagement.Application.DTOs;
using DebtManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DebtManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request.Email, request.Password);
        if (result == null)
            return Unauthorized(new { error = "Email hoặc mật khẩu không đúng" });
        return Ok(result);
    }
}

