# VCB DebtFlow - Full .NET

- Frontend: Blazor Server / Interactive Server (.NET 8)
- Backend: ASP.NET Core Application Services
- Database: MySQL `vcb_debt_management`
- Timezone: GMT+7 (`Asia/Bangkok`)

React, Vite, Node.js, npm and Supabase are no longer part of the active project.

## Run

```powershell
cd D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\frontend\DebtManagement.BlazorApp
dotnet run
```

URL: `http://localhost:5050`

## Database update

```powershell
cd D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\backend\DebtManagement.Api
dotnet ef database update --project ..\DebtManagement.Infrastructure\DebtManagement.Infrastructure.csproj --startup-project .
```

## Test API

```powershell
powershell.exe -ExecutionPolicy Bypass -File D:\web\VCB_QuanLyChungTu\FE\tests\ApiIntegrationTests.ps1 -Cleanup
```

## IIS

See:

```text
D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\frontend\DebtManagement.BlazorApp\docs\IIS_DEPLOYMENT.md
```
