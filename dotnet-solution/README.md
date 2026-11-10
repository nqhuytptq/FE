# Hệ thống Quản lý và Theo dõi Văn bản nợ luân chuyển

## Kiến trúc tổng thể

Hệ thống được định hướng theo mô hình **ASP.NET Core + Blazor WebAssembly**, tổ chức
thành một .NET Solution đa tầng (Clean Architecture).

> **Lưu ý thực thi:** Bản demo chạy thực tế sử dụng **Vite + React + TypeScript** với
> **Supabase** làm backend (PostgreSQL + RLS). Cấu trúc thư mục .NET dưới đây mô phỏng
> kiến trúc mục tiêu — mỗi tầng tương ứng với các module logic trong mã nguồn React.

## Cấu trúc Solution

```
dotnet-solution/
├── DebtDocumentManagement.sln          # Solution file
├── backend/
│   ├── DebtManagement.Domain/           # Thực thể nghiệp vụ (Models)
│   ├── DebtManagement.Application/      # Use-cases, DTOs, interfaces
│   ├── DebtManagement.Infrastructure/   # EF Core, repository implementations
│   └── DebtManagement.Api/             # ASP.NET Core Web API (controllers, middleware)
└── frontend/
    └── DebtManagement.BlazorApp/        # Blazor WebAssembly UI
```

## Phân tầng

| Tầng | Trách nhiệm | Tương ứng trong demo |
|------|------------|---------------------|
| Domain | Entities: `DebtDocument`, `Department`, `AppUser`, `CirculationHistory` | `src/types/` + Supabase schema |
| Application | DTOs, validators, service interfaces | `src/lib/` + `src/hooks/` |
| Infrastructure | DbContext, repositories, migrations | Supabase (Postgres + RLS) |
| API | REST controllers, auth, Swagger | Supabase Data API (auto-generated) |
| BlazorApp | Components, pages, state management | `src/components/` + `src/pages/` |

## Luồng luân chuyển văn bản nợ

```
Phòng Khách hàng → Phòng Quản lý nợ → Phòng Kế toán
     (Khởi tạo)      (Đánh giá/Duyệt)     (Ghi nhận/Thanh toán)
```

Trạng thái: `Chờ duyệt` → `Đã duyệt` / `Quá hạn` / `Từ chối`
