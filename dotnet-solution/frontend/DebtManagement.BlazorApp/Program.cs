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
builder.Services.AddScoped<IDocumentAuditService, DocumentAuditService>();
builder.Services.AddScoped<IDebtItemVerificationService, DebtItemVerificationService>();
builder.Services.AddScoped<IDebtWorkflowService, DebtWorkflowService>();
builder.Services.AddScoped<IDebtDocumentService, DebtDocumentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<BlazorSession>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();
