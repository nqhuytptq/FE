# VCB DebtFlow - Hệ thống quản lý và luân chuyển hồ sơ văn bản nợ

## Kiến trúc

```text
Blazor Server / Interactive Server
        ↓
ASP.NET Core Services
        ↓
Entity Framework Core + Pomelo MySQL
        ↓
MySQL: vcb_debt_management
```

Dự án hiện chạy hoàn toàn bằng .NET. Không sử dụng React, Vite, Node.js hoặc Supabase.

## Yêu cầu

- Windows 10/11 hoặc Windows Server
- .NET 8 SDK
- MySQL Server port 3306
- Database `vcb_debt_management`

## Chạy local

```powershell
cd D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\frontend\DebtManagement.BlazorApp
dotnet run
```

Mở:

```text
http://localhost:5050
```

## Tài khoản test

```text
admin@vcb.com / Admin@123456
linhdtl.tah@vietcombank.com.vn / 123456a@
haitm.tah@vietcombank.com.vn / 123456a@
anhntl.tah@vietcombank.com.vn / 123456a@
dangnx.tah@vietcombank.com.vn / 123456a@
cuongcnc.tah@vietcombank.com.vn / 123456a@
hoapt.tah@vietcombank.com.vn / 123456a@
```

## Database migration

```powershell
cd D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\backend\DebtManagement.Api

dotnet ef database update `
  --project ..\DebtManagement.Infrastructure\DebtManagement.Infrastructure.csproj `
  --startup-project .
```

## Build

```powershell
dotnet build D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\DebtManagement.sln
```

## Publish IIS

```powershell
cd D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\frontend\DebtManagement.BlazorApp
dotnet publish -c Release -o D:\publish\VCB-DebtManagement
```

Tài liệu IIS:

```text
dotnet-solution/frontend/DebtManagement.BlazorApp/docs/IIS_DEPLOYMENT.md
```

## Kiểm thử API

```powershell
powershell.exe -ExecutionPolicy Bypass `
  -File D:\web\VCB_QuanLyChungTu\FE\tests\ApiIntegrationTests.ps1 `
  -Cleanup
```

## Cấu trúc

```text
dotnet-solution/
├── DebtManagement.sln
├── backend/
│   ├── DebtManagement.Domain/
│   ├── DebtManagement.Application/
│   ├── DebtManagement.Infrastructure/
│   └── DebtManagement.Api/
└── frontend/
    └── DebtManagement.BlazorApp/
```

## Workflow

```text
PKH → PQLN → PDV
```

- NV PKH khởi tạo hồ sơ.
- SUP PKH duyệt.
- NV PQLN tiếp nhận và xử lý chứng từ.
- SUP PQLN duyệt.
- NV PDV tiếp nhận và đi bút toán.
- SUP PDV duyệt hoàn tất một hoặc hai lần tùy tình trạng chứng từ nợ.

Múi giờ hiển thị: GMT+7 (`Asia/Bangkok`, tương đương Hà Nội và TP. Hồ Chí Minh).
