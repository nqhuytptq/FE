# Publish Blazor Server lên Windows Server IIS

## 1. Chuẩn bị

Cài đặt trên Windows Server:

- .NET 8 ASP.NET Core Hosting Bundle
- IIS Web Server
- IIS WebSocket Protocol
- Kết nối tới MySQL `vcb_debt_management`

Sau khi cài Hosting Bundle:

```powershell
iisreset
```

## 2. Publish

```powershell
cd D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\frontend\DebtManagement.BlazorApp
dotnet publish -c Release -o D:\publish\VCB-DebtManagement
```

## 3. IIS Application Pool

- Name: `VCB-DebtManagement`
- .NET CLR Version: `No Managed Code`
- Pipeline: `Integrated`
- Start Mode: `AlwaysRunning` nếu production

## 4. IIS Website

1. Chọn **Sites → Add Website**.
2. Site name: `VCB-DebtManagement`.
3. Physical path: `D:\publish\VCB-DebtManagement`.
4. Chọn Application Pool `VCB-DebtManagement`.
5. Cấu hình HTTP/HTTPS và DNS nội bộ.

## 5. Connection string production

Không dùng mật khẩu root trên production. Tạo user MySQL riêng cho ứng dụng và cấu hình `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=MYSQL-SERVER;Port=3306;Database=vcb_debt_management;Uid=vcb_app;Pwd=CHANGE_ME;"
  }
}
```

## 6. Kiểm tra sau deploy

- Đăng nhập admin.
- Đăng nhập NV PKH/PQLN/PDV.
- Kiểm tra hồ sơ chờ tiếp nhận.
- Kiểm tra workflow PKH → PQLN → PDV.
- Kiểm tra timeline và chứng từ nợ.
- Kiểm tra múi giờ GMT+7.

## 7. Lỗi thường gặp

### HTTP 500.30

Kiểm tra Hosting Bundle, connection string và Event Viewer.

### SignalR/WebSocket lỗi

Cài và bật IIS WebSocket Protocol.

### Không kết nối MySQL

```powershell
Test-NetConnection MYSQL-SERVER -Port 3306
```
