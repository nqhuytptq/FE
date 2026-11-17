# DebtManagement .NET Solution

Full .NET application using Blazor Server, ASP.NET Core services, EF Core and MySQL.

No React, Vite, Node.js or Supabase are required.

## Run locally

```powershell
cd D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\frontend\DebtManagement.BlazorApp
dotnet run
```

Open `http://localhost:5050`.

## Build

```powershell
dotnet build D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\DebtManagement.sln
```

## Publish IIS

```powershell
cd D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\frontend\DebtManagement.BlazorApp
dotnet publish -c Release -o D:\publish\VCB-DebtManagement
```

See `frontend/DebtManagement.BlazorApp/docs/IIS_DEPLOYMENT.md`.
