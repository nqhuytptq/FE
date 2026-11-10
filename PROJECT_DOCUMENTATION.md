# TÀI LIỆU TOÀN DIỆN HỆ THỐNG QUẢN LÝ VÀ THEO DÕI VĂN BẢN NỢ LUÂN CHUYỂN (VCB)

> **Phiên bản tài liệu:** 3.1 (Múi giờ GMT+7 Asia/Bangkok + Quản lý Đếm Chứng từ nợ + NV PQLN thêm chứng từ)  
> **Cập nhật:** Tháng 11/2026  
> **Backend:** ASP.NET Core 8 Web API + Entity Framework Core + Pomelo MySQL  
> **Frontend:** React 18 + TypeScript + Vite 7 + Tailwind CSS  
> **Database:** MySQL Local (`localhost:3306`, database `vcb_debt_management`)

---

## 1. CÁC THAY ĐỔI NGHIỆP VỤ MỚI

1. **Bỏ hoàn toàn mục "Số tiền nợ":**
   - Đã xóa trường `DebtAmount` / `debt_amount` khỏi database, backend models, DTOs, API và giao diện.
   - Thay thế bằng **Số chứng từ nợ** (được đếm tự động từ danh sách chứng từ nợ người dùng đã nhập).
   - Thẻ thống kê Dashboard chuyển từ *"Tổng nợ đang theo dõi"* thành **"Tổng chứng từ nợ"** (tổng số lượng chứng từ nợ đã khai báo).

2. **Nhân viên PQLN được quyền thêm chứng từ nợ:**
   - Cán bộ PQLN sau khi bấm **"Tiếp nhận hồ sơ"** sẽ xuất hiện khung chức năng: **"PQLN: Thêm chứng từ nợ vào hồ sơ"**.
   - Cán bộ nhập tên chứng từ nợ bổ sung và bấm **"+ Thêm chứng từ"**.
   - Hệ thống gọi API `POST /api/documents/{id}/debt-items`, tự động thêm vào danh mục chứng từ của hồ sơ và ghi vết vào Luồng theo dõi hồ sơ (`CirculationHistory`).
   - Cán bộ PQLN cũng có quyền tích chọn bổ sung các chứng từ này khi khách hàng nộp đủ.

3. **Chuẩn hóa múi giờ GMT+7 (Bangkok, Hà Nội, TP. Hồ Chí Minh):**
   - Backend lưu trữ UTC chuẩn ISO.
   - Frontend hiển thị 100% thời gian theo múi giờ `Asia/Bangkok` (GMT+7, không có Daylight Saving Time).
   - Ngày mặc định trên form tạo hồ sơ lấy chính xác theo ngày hiện tại GMT+7 (`getTodayDateGmt7()`), không bị nhảy lùi ngày do UTC.

---

## 2. DANH SÁCH TÀI KHOẢN NGƯỜI DÙNG CHUẨN VCB TAH

> Mật khẩu mặc định cho các tài khoản test là: **`123456a@`**

| Tên người dùng | Email đăng nhập | Phòng ban | Chức vụ (Role) |
|---|---|---|---|
| **Dương Tống Lam Linh** | `linhdtl.tah@vietcombank.com.vn` | Phòng Khách hàng (PKH) | Nhân viên (`staff`) |
| **Trần Minh Hải** | `haitm.tah@vietcombank.com.vn` | Phòng Khách hàng (PKH) | Lãnh đạo phòng (`manager`) |
| **Nguyễn Thị Lan Anh** | `anhntl.tah@vietcombank.com.vn` | Phòng Quản lý nợ (PQLN) | Nhân viên (`staff`) |
| **Nguyễn Xuân Đang** | `dangnx.tah@vietcombank.com.vn` | Phòng Quản lý nợ (PQLN) | Lãnh đạo phòng (`manager`) |
| **Cao Nam Cường** | `cuongcnc.tah@vietcombank.com.vn` | Phòng Dịch vụ khách hàng (PDV) | Nhân viên (`staff`) |
| **Phạm Thanh Hoa** | `hoapt.tah@vietcombank.com.vn` | Phòng Dịch vụ khách hàng (PDV) | Lãnh đạo phòng (`manager`) |
| **Quản trị viên** | `admin@vcb.com` (`Admin@123456`) | Toàn hệ thống | Quản trị viên (`admin`) |

---

## 3. QUY TRÌNH LUÂN CHUYỂN 3 PHÒNG BAN

```
[NV PKH: Khởi tạo & Nhập CT nợ] ──> [SUP PKH: Duyệt chuyển PQLN]
                                              │
                                              ▼
                             [NV PQLN: Tiếp nhận & Có quyền thêm/bổ sung CT nợ] ──> [SUP PQLN: Duyệt chuyển PDV]
                                                                                              │
                                                                                              ▼
                                                                                 [NV PDV: Tiếp nhận & Đi bút toán]
                                                                                              │
                                                                                              ▼
                                                                                 [SUP PDV: Phê duyệt hoàn tất]
                                                                                  ├── Luồng 1 (Có nợ CT): Duyệt lần 1 -> Bổ sung đủ CT -> Duyệt lần 2 -> HOÀN TẤT
                                                                                  └── Luồng 2 (Đủ/Không nợ CT): Duyệt 1 lần -> HOÀN TẤT
```

---

## 4. BẢNG ENDPOINTS REST API

Base URL: `http://localhost:5000/api`

| Method | Endpoint | Quyền hạn | Chức năng |
|---|---|---|---|
| `POST` | `/auth/login` | Public | Đăng nhập lấy JWT Bearer token |
| `GET` | `/departments` | Authenticated | Danh sách 3 phòng ban |
| `GET` | `/users` | Authenticated | Danh sách người dùng |
| `GET` | `/documents` | Authenticated | Danh sách văn bản nợ & đếm số chứng từ nợ |
| `GET` | `/documents/{id}` | Authenticated | Chi tiết văn bản, danh sách CT nợ & lịch sử |
| `POST` | `/documents` | NV PKH | Khởi tạo văn bản nợ kèm danh mục CT nợ |
| `PUT` | `/documents/{id}` | NV PKH / PQLN | Chỉnh sửa thông tin văn bản |
| `POST` | `/documents/{id}/pkh-sup-approve` | SUP PKH | Duyệt và chuyển sang PQLN |
| `POST` | `/documents/{id}/pqln-receive` | NV PQLN | Tiếp nhận hồ sơ tại PQLN |
| **`POST`** | **`/documents/{id}/debt-items`** | **NV PQLN** | **Thêm chứng từ nợ mới vào hồ sơ** |
| `POST` | `/documents/{id}/pqln-submit-sup` | NV PQLN | Đẩy hồ sơ lên SUP PQLN duyệt |
| `POST` | `/documents/{id}/pqln-sup-approve` | SUP PQLN | Duyệt và chuyển sang PDV |
| `POST` | `/documents/{id}/pdv-receive` | NV PDV | Tiếp nhận hồ sơ tại PDV |
| `POST` | `/documents/{id}/pdv-post-entry` | NV PDV | Thực hiện đi bút toán |
| `POST` | `/documents/{id}/pdv-sup-approve` | SUP PDV | Duyệt hoàn tất (Lần 1 / Lần 2) |
| `POST` | `/documents/{id}/supplement` | NV PKH / PQLN | Tích chọn bổ sung chứng từ nợ |
| `POST` | `/documents/{id}/return` | Các phòng | Trả lại hồ sơ |
| `POST` | `/documents/{id}/reject` | Lãnh đạo phòng | Từ chối hồ sơ |

---

## 5. KỊCH BẢN KIỂM THỬ TỰ ĐỘNG

Chạy lệnh kiểm thử toàn diện:
```powershell
powershell.exe -ExecutionPolicy Bypass -File D:\web\VCB_QuanLyChungTu\FE\tests\ApiIntegrationTests.ps1 -Cleanup
```
**Kết quả kiểm thử:** 29 / 29 Test Cases **PASSED 100%**.

---

## 6. HƯỚNG DẪN KHỞI CHẠY HỆ THỐNG

### Terminal 1 - Backend:
```powershell
cd D:\web\VCB_QuanLyChungTu\FE\dotnet-solution\backend\DebtManagement.Api
dotnet run
```
API: `http://localhost:5000` | Swagger: `http://localhost:5000/swagger`

### Terminal 2 - Frontend:
```powershell
cd D:\web\VCB_QuanLyChungTu\FE
npm.cmd run dev
```
Web App: `http://localhost:5173`
