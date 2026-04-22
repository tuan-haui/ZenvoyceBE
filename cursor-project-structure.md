# THÔNG TIN DỰ ÁN: HỆ THỐNG QUẢN LÝ HÓA ĐƠN ĐIỆN TỬ (E-INVOICE)

## 1. TỔNG QUAN CÔNG NGHỆ (TECH STACK)
- **Nền tảng:** .NET 9 (C# 13)
- **Kiến trúc:** Clean Architecture + CQRS Pattern
- **Database:** PostgreSQL
- **ORM:** Entity Framework Core 9 (EF Core 9) - Database First (Tự động map Entity)
- **Thư viện cốt lõi:**
  - `MediatR` (Xử lý luồng CQRS)
  - `AutoMapper` (Ánh xạ Entity <-> DTO)
  - `FluentValidation` (Validate dữ liệu đầu vào)
  - `Serilog` (Ghi log hệ thống)
  - `Scalar.AspNetCore` & `Microsoft.AspNetCore.OpenApi` (Giao diện API Document)
  - `System.IdentityModel.Tokens.Jwt` (Xử lý xác thực Token)
  - `BouncyCastle.Cryptography` (Xử lý ký số)
  - `MailKit` (Gửi email hóa đơn)

## 2. CẤU TRÚC CLEAN ARCHITECTURE (QUY TẮC BẤT BIẾN)
Dự án chia làm 4 layer. AI phải đặt file đúng thư mục và tuân thủ quy tắc tham chiếu (Dependency Rule):

1. **Domain Layer (`Domain` project):**
   - Chứa Entities (POCO), Enums, Custom Exceptions, Interfaces.
   - **Quy tắc:** Tuyệt đối KHÔNG phụ thuộc vào bất kỳ thư viện ngoại vi nào (Không EF Core, không ASP.NET).

2. **Application Layer (`Application` project):**
   - Chứa Use Cases (CQRS: Commands/Queries), DTOs, FluentValidation, Interfaces của Infrastructure (như `IEmailService`, `ISignatureService`).
   - Phụ thuộc: `Domain`.

3. **Infrastructure Layer (`Infrastructure` project):**
   - Chứa `EInvoiceDbContext` (EF Core), cấu hình PostgreSQL.
   - Chứa các class implement interface (Ví dụ: `EmailService`, `JwtService`).
   - Phụ thuộc: `Application`, `Domain`.

4. **Presentation / API Layer (`Api` project):**
   - Chứa Controllers, Middlewares, DI Setup (`Program.cs`).
   - Phụ thuộc: `Application`, `Infrastructure`.
   - **Quy tắc:** Tuyệt đối KHÔNG inject `EInvoiceDbContext` trực tiếp vào Controller. Chỉ inject `IMediator` (hoặc `ISender`).

## 3. QUY TẮC LẬP TRÌNH (CODING STANDARDS) CHO AI
Khi được yêu cầu tạo tính năng CRUD hoặc API mới, AI phải tuân thủ các bước sau:

- **ID & Khóa chính:** Sử dụng `Guid` (UUID) cho tất cả Khóa chính. ID phải được tạo từ Backend (`Guid.NewGuid()`) trước khi Insert, không để Database tự sinh.
- **Trường hệ thống (System Fields):** Mọi thao tác Insert/Update phải cập nhật các trường: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`.
- **Xóa mềm (Soft Delete):** Các thao tác Delete chỉ được cập nhật cờ `IsDeleted = true`, KHÔNG dùng lệnh `.Remove()` trong EF Core.
- **CQRS Workflow:**
  1. Tạo Record `Command`/`Query` trong `Application/FeatureName/Commands` hoặc `Queries`.
  2. Tạo Validator dùng `AbstractValidator` trong cùng thư mục.
  3. Tạo `Handler` implement `IRequestHandler`. Handler là nơi gọi `EInvoiceDbContext` để query/save DB.
  4. Tạo class DTO Response và cấu hình `AutoMapper` Profile.
  5. Trong Controller (API), tạo các Endpoint (Route `api/[controller]`) và gọi `await _mediator.Send(command)`. Returns chuẩn RESTful (200 OK, 201 Created, 400 BadRequest).

## 4. TỔNG QUAN CƠ SỞ DỮ LIỆU (DATABASE SCHEMA)
Các bảng nghiệp vụ chính đã được thiết kế sẵn (EF Core đã Reverse Engineer thành Entities):

- **Hệ thống & Phân quyền:** `Nguoidung` (User), `Nhomquyen` (Role), `Sysmenu` (Menu), `PhanQuyenChucNang`.
- **Danh mục:** `TTcty` (Công ty phát hành), `TTkhachhang` (Khách hàng mua), `Danhmuchanghoa` (Sản phẩm/Dịch vụ).
- **Hóa đơn (Lõi):**
  - `TTHoadon`: Bảng cha lưu hóa đơn (Tổng tiền, Ngày lập, Trạng thái: Draft, Signed, Issued, Cancelled). Có trường `XMLDaKy` lưu file XML gốc.
  - `TTHangHoa`: Bảng con lưu chi tiết các mặt hàng của hóa đơn (Số lượng, Đơn giá, Thành tiền).
- **Mẫu hóa đơn:** `MauhoadonGoc`, `Mauchocty`, `ThongtinHDMau`.
- **Lưu vết & Log:** `LichsuHoadon` (Vòng đời hóa đơn), `Lichsuguithue` (Giao tiếp cơ quan thuế), `QLKyso` (Log chứng thư số).

## 5. CÁC NGHIỆP VỤ CỐT LÕI (CORE BUSINESS LOGIC)
- **Xác thực:** Dùng JWT Bearer. Bắt buộc truyền Token qua Header.
- **Tạo Hóa Đơn:** Lưu `TTHoadon` và danh sách `TTHangHoa` trong cùng một Transaction (Dùng EF Core `SaveChangesAsync`).
- **Ký số Hóa Đơn:** Hóa đơn sau khi tạo nháp sẽ được ký số. Cần ghi log vào bảng `QLKyso`.
- **Lưu vết thao tác:** Bất kỳ sự thay đổi trạng thái nào của Hóa đơn đều phải được insert một bản ghi vào bảng `LichsuHoadon`.