# Cấu hình phát triển API Backend - Phân hệ 1: Quản trị hệ thống và Người dùng

## 1. Yêu cầu Kiến trúc & Công nghệ (Core Constraints)
- **Framework:** .NET 9 [1].
- **Kiến trúc:** Clean Architecture (Domain, Application, Infrastructure, API) [1].
- **Pattern:** CQRS sử dụng `MediatR` [1]. Dữ liệu trả về qua DTO dùng `AutoMapper`.
- **Database:** PostgreSQL với `Entity Framework Core 9` [1].
- **Khóa chính (PK):** Bắt buộc sử dụng `Guid` cho toàn bộ các ID [1, 2]. Khởi tạo Guid từ tầng Application trước khi Insert.
- **Bảo mật:** JWT Bearer Authentication. Mật khẩu phải được mã hóa bằng `BCrypt` trước khi lưu vào DB [1].
- **Lưu vết & Xóa mềm (System Tracking):** Tất cả các bảng phải triển khai Soft Delete (`Is_Deleted = boolean`) và lưu vết (`Created_At`, `Updated_At`, `Created_By`, `Updated_By`) [1, 2].

## 2. Thực thể dữ liệu tầng Domain (Domain Entities)
Tạo các class Entity sau bằng C# thuần:
1. **Nguoidung (Người dùng):** `ID` (Guid), `Madonvi` (Guid - FK), `Tendangnhap` (string), `Matkhau` (string - Hash), `Dienthoai` (string), `Trangthai` (int: 1-Active, 0-Inactive) [2, 3].
2. **Nhomquyen (Nhóm quyền):** `ID` (Guid), `Tenquyen` (string), `Mota` (string) [3, 4].
3. **Sysmenu (Menu hệ thống):** `ID` (Guid), `Tenmenu` (string), `Duongdan` (string), `MenuchaID` (Guid?), `QuyenID` (Guid - FK) [3, 4].
4. **PhanQuyenChucNang (Bảng trung gian):** `NguoidungID` (Guid), `QuyenID` (Guid), `MenuID` (Guid) [3, 5].

## 3. Đặc tả API Endpoints & Logic (CQRS)

### UC01: Đăng nhập và Đăng xuất [3, 6, 7]
*Mô tả: Xác thực người dùng và cấp phát Token truy cập.*
- **POST /api/auth/login** (LoginCommand)
  - **Payload:** `{ Username, Password }`
  - **Logic:**
    1. Tìm `Nguoidung` theo `Tendangnhap` và `Is_Deleted == false`.
    2. Xác thực `Password` bằng `BCrypt`.
    3. Kiểm tra `Trangthai == 1` (Đang hoạt động) [7, 8].
    4. Sinh `JWT Token` chứa thông tin UserID, QuyenID. Hết hạn sau 15 phút [8, 9].
  - **Response:** `{ Token, ExpiredAt, UserInfo }`
- **POST /api/auth/logout** (LogoutCommand)
  - **Logic:** Hủy phiên làm việc phía Client [7, 8].

### UC02: Quản lý tài khoản người dùng [6, 10, 11]
*Mô tả: Admin cấp phát tài khoản (hệ thống On-premise, không có luồng tự đăng ký) và người dùng cập nhật thông tin.*
- **GET /api/users** (GetAllUsersQuery / GetUserByIdQuery)
  - **Logic:** Lấy danh sách tài khoản (chỉ lấy `Is_Deleted == false`), hỗ trợ phân trang.
- **POST /api/users** (CreateUserCommand)
  - **Logic:** Kiểm tra trùng `Tendangnhap` hoặc `Email` [10, 11]. Hash mật khẩu bằng BCrypt trước khi lưu vào DB.
- **PUT /api/users/{id}** (UpdateUserCommand)
  - **Logic:** Cập nhật thông tin (Họ tên, email, sđt) [10, 11].
- **PATCH /api/users/{id}/change-password** (ChangePasswordCommand)
  - **Logic:** Yêu cầu xác thực mật khẩu cũ. Kiểm tra độ mạnh mật khẩu mới và cập nhật Hash [10, 11].
- **DELETE /api/users/{id}** (DeleteUserCommand)
  - **Logic:** Cập nhật cờ `Is_Deleted = true` (Không dùng Hard Delete) [2, 11].

### UC03 & UC04: Quản lý nhóm quyền và Phân quyền [6, 11-13]
*Mô tả: Định nghĩa các Role và gán quyền truy cập chức năng.*
- **GET /api/roles** (GetRolesQuery)
- **POST /api/roles** (CreateRoleCommand)
  - **Logic:** Kiểm tra trùng `Tenquyen` [12].
- **PUT /api/roles/{id}/assign-permissions** (AssignPermissionsCommand)
  - **Payload:** `{ RoleId, UserId, List<MenuId> }`
  - **Logic:** Cập nhật bảng `PhanQuyenChucNang`. Xóa cấu hình cũ của User/Role và Insert cấu hình mới trong cùng 1 Transaction [13, 14].

### UC05: Quản lý menu hệ thống [6, 14, 15]
*Mô tả: Định nghĩa thanh điều hướng Sidebar.*
- **GET /api/menus/sidebar** (GetSidebarQuery)
  - **Logic:** Lấy danh sách Menu được phép truy cập dựa vào `QuyenID` của user đang đăng nhập (lấy từ JWT Token) [14].
- **POST /api/menus** (CreateMenuCommand)
  - **Logic:** Kiểm tra trùng `Duongdan` (Route path). Lưu thông tin Icon, Thứ tự [14, 15].

### Đặc tả Log hệ thống (Audit Log) [16, 17]
- **GET /api/system/logs** (GetAuditLogsQuery)
  - **Logic:** Truy vấn lịch sử thao tác của người dùng, lọc theo `Từ ngày - Đến ngày`, `UserId`, `Loại thao tác`. 

## 4. Ràng buộc Validation (Dùng FluentValidation) [17]
- `Tendangnhap`: Bắt buộc nhập, không chứa khoảng trắng, tối thiểu 5 ký tự.
- `Matkhau`: Bắt buộc nhập, phải đạt độ phức tạp (chữ hoa, chữ thường, số, ký tự đặc biệt).
- `Email`: Phải đúng định dạng chuẩn.

## 5. Hướng dẫn sinh Code cho Cursor (Cursor Instructions)
Khi đọc file này, hãy tuân thủ cấu trúc thư mục sau:
1. **Domain:** Tạo các class Entities.
2. **Application:**
   - Tạo thư mục `Features/{EntityName}`.
   - Bên trong tạo `Commands`, `Queries`, `DTOs`.
   - Viết các `IRequestHandler` và cấu hình `FluentValidation`.
3. **Infrastructure:** Cấu hình `EntityTypeConfiguration` cho EF Core (nhớ filter `Is_Deleted == false`).
4. **Api:** Tạo `Controllers` kế thừa `ControllerBase`, Inject `IMediator`, gắn tag `[Authorize]` và định tuyến chuẩn RESTful.