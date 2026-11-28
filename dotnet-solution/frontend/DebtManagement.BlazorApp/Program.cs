using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

﻿using DebtManagement.Application.Interfaces;
using DebtManagement.Application.Services;
using DebtManagement.BlazorApp;
using DebtManagement.BlazorApp.Components;
using DebtManagement.Infrastructure.Data;
using DebtManagement.Infrastructure.Repositories;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' missing");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddScoped<IDebtDocumentRepository, DebtDocumentRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDebtDocumentService, DebtDocumentService>();
builder.Services.AddScoped<ISupReportService, SupReportService>();
builder.Services.AddScoped<IDocumentAuditService, DocumentAuditService>();
builder.Services.AddScoped<IDebtItemVerificationService, DebtItemVerificationService>();
builder.Services.AddScoped<IDebtWorkflowService, DebtWorkflowService>();
builder.Services.AddScoped<IDebtDocumentService, DebtDocumentService>();
builder.Services.AddScoped<ISupReportService, SupReportService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<BlazorSession>();
builder.Services.AddScoped<IKpiRepository, KpiRepository>();
builder.Services.AddScoped<IWorkingCalendarService, WorkingCalendarService>();
builder.Services.AddScoped<IKpiCalculationService, KpiCalculationService>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var requiredDepts = new[]
    {
        new { Code = "KHDN", Name = "Phòng Khách hàng Doanh nghiệp", Desc = "Khởi tạo và quản lý hồ sơ khách hàng doanh nghiệp", Sort = 1 },
        new { Code = "KHBL", Name = "Phòng Khách hàng Bán lẻ", Desc = "Khởi tạo và quản lý hồ sơ khách hàng cá nhân/bán lẻ", Sort = 2 },
        new { Code = "PGD_LLQ", Name = "PGD Lạc Long Quân", Desc = "Phòng giao dịch Lạc Long Quân", Sort = 3 },
        new { Code = "PGD_CD", Name = "PGD Cửa Đông", Desc = "Phòng giao dịch Cửa Đông", Sort = 4 },
        new { Code = "PQLN", Name = "Phòng Quản lý nợ", Desc = "Thẩm định, tiếp nhận và kiểm soát chứng từ nợ", Sort = 5 },
        new { Code = "PDV", Name = "Phòng Dịch vụ khách hàng", Desc = "Tiếp nhận, đi bút toán và hoàn tất hồ sơ", Sort = 6 }
    };

    var existingDepts = db.Departments.ToList();
    var changed = false;

    foreach (var item in requiredDepts)
    {
        var found = existingDepts.FirstOrDefault(d => d.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
        if (found == null)
        {
            db.Departments.Add(new DebtManagement.Domain.Entities.Department
            {
                Id = Guid.NewGuid(),
                Code = item.Code,
                Name = item.Name,
                Description = item.Desc,
                SortOrder = item.Sort,
                CreatedAt = DateTime.UtcNow
            });
            changed = true;
        }
        else if (found.SortOrder != item.Sort)
        {
            found.SortOrder = item.Sort;
            changed = true;
        }
    }

    var legacyPkh = existingDepts.FirstOrDefault(d => d.Code.Equals("PKH", StringComparison.OrdinalIgnoreCase));
    if (legacyPkh != null && legacyPkh.SortOrder != 99)
    {
        legacyPkh.SortOrder = 99;
        changed = true;
    }

    if (changed)
        db.SaveChanges();
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();
