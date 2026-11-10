# Hệ Thống Quản Lý & Theo Dõi Văn Bản Nợ Luân Chuyển (VCB)

> Dự án chạy hoàn toàn **Local**: **React + Vite** (FE) ↔ **ASP.NET Core 8 Web API** (BE) ↔ **MySQL Local** (DB).

---

## ⚡ Hướng Dẫn Chạy Nhanh

### 1. Chuẩn bị MySQL
- Đảm bảo MySQL đang chạy ở `localhost:3306`
- Database: `vcb_debt_management`
- User/Pass: `root` / `24082002` (cấu hình trong `dotnet-solution/backend/DebtManagement.Api/appsettings.json`)

### 2. Chạy Backend (Terminal 1)
```powershell
cd D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\backend\DebtManagement.Api
dotnet run
```
API chạy tại: `http://localhost:5000`  
Swagger UI: `http://localhost:5000/swagger`

### 3. Chạy Frontend (Terminal 2)
```powershell
cd D:\web\VCB_QuanLyChungTu\FE
npm.cmd run dev
```
Truy cập: `http://localhost:5173`

### 4. Đăng nhập
- **Email:** `admin@vcb.com`
- **Mật khẩu:** `Admin@123456`

---

## 📖 Tài Liệu Chi Tiết

Xem file **[PROJECT_DOCUMENTATION.md](./PROJECT_DOCUMENTATION.md)** để đọc:
- Chi tiết kiến trúc Clean Architecture
- Cấu trúc 5 bảng CSDL MySQL
- Tài liệu toàn bộ REST Endpoints
- Quy trình luân chuyển PKH → PQN → PKT
- Kịch bản kiểm thử và hướng dẫn xử lý lỗi
