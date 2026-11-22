using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManagement.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}", traceId, context.Request.Path);
            await HandleExceptionAsync(context, ex, traceId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string traceId)
    {
        var (statusCode, title, detail) = exception switch
        {
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Không có quyền truy cập",
                exception.Message),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "Không tìm thấy dữ liệu",
                exception.Message),

            ArgumentException or InvalidOperationException => (
                HttpStatusCode.BadRequest,
                "Dữ liệu hoặc thao tác không hợp lệ",
                exception.Message),

            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                "Xung đột cập nhật dữ liệu",
                "Chứng từ này vừa được người dùng khác cập nhật. Vui lòng tải lại trang và thử lại."),

            _ => (
                HttpStatusCode.InternalServerError,
                "Lỗi hệ thống nội bộ",
                _env.IsDevelopment()
                    ? exception.ToString()
                    : "Đã xảy ra lỗi trong quá trình xử lý. Vui lòng liên hệ quản trị hệ thống kèm mã tra cứu.")
        };

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = traceId;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}