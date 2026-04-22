# Cấu hình phát triển API Backend - Phân hệ 2: Quản lý Danh mục

## 1. Tổng quan kỹ thuật (Technical Stack)
- **Framework:** .NET 9.
- **Kiến trúc:** Clean Architecture (Domain, Application, Infrastructure, API).
- **Pattern:** CQRS sử dụng `MediatR` và `AutoMapper`.
- **Cơ sở dữ liệu:** PostgreSQL với `Entity Framework Core 9`.
- **Kiểu dữ liệu ID:** Bắt buộc sử dụng **Guid** (UUID), được khởi tạo từ tầng Application trước khi Insert.
- **Lưu vết & Xóa mềm:** Mọi entity phải kế thừa `BaseEntity` chứa các trường: `Created_At`, `Updated_At`, `Created_By`, `Updated_By` và cờ `Is_Deleted` (Soft Delete).

## 2. Thực thể dữ liệu tầng Domain (Domain Entities)
Tạo các class Entity sau trong project Domain:

1. **TTcty (Công ty phát hành):** `ID` (Guid), `MasoThue` (string - Unique), `Tendonvi` (string), `Diachi` (string), `Dienthoai` (string), `Trangthai` (int: 1-Kích hoạt, 0-Vô hiệu hóa) [6].
2. **TTkhachhang (Khách hàng):** `ID` (Guid), `DonviID` (Guid - FK liên kết TTcty), `Tenkhachhang` (string), `MasoThue` (string), `Email` (string), `Dienthoai` (string) [7].
3. **Danhmuchanghoa (Hàng hóa/Dịch vụ):** `ID` (Guid), `DonviID` (Guid - FK liên kết TTcty), `Tenhanghoa` (string), `Donvitinh` (string), `Dongia` (decimal), `Thuesuat` (decimal) [7].

## 3. Đặc tả API Endpoints & Logic (CQRS)

### UC06: Quản lý Công ty (TTcty)
*Mô tả: Khởi tạo và quản lý thông tin doanh nghiệp phát hành hóa đơn.*
- **GET /api/companies** (GetCompaniesQuery / GetCompanyByIdQuery)
  - **Logic:** Lấy danh sách công ty (chỉ lấy `Is_Deleted == false`).
- **POST /api/companies** (CreateCompanyCommand)
  - **Payload:** `{ MasoThue, Tendonvi, Diachi, Dienthoai }`
  - **Logic:** Kiểm tra trùng `MasoThue`. Sinh Guid cho ID, set `Trangthai = 1`. Tự động cấu hình đây là công ty mặc định phát hành hóa đơn [3].
- **PUT /api/companies/{id}** (UpdateCompanyCommand)
  - **Logic:** Cập nhật thông tin. **Ràng buộc quan trọng:** Kiểm tra nếu công ty đã phát hành hóa đơn (có dữ liệu trong bảng `TTHoadon`), thì KHÔNG cho phép cập nhật `MasoThue` và `Tendonvi` [3].
- **PATCH /api/companies/{id}/status** (ChangeCompanyStatusCommand)
  - **Logic:** Thay đổi trạng thái (Kích hoạt/Vô hiệu hóa) để cho phép hoặc ngừng phát hành hóa đơn [3].

### UC07: Quản lý Khách hàng (TTkhachhang)
*Mô tả: Quản lý tệp khách hàng theo từng công ty.*
- **GET /api/companies/{donviId}/customers** (GetCustomersByCompanyQuery)
  - **Logic:** Lấy danh sách khách hàng thuộc về một công ty cụ thể, hỗ trợ tìm kiếm theo tên hoặc mã số thuế [4].
- **POST /api/customers** (CreateCustomerCommand)
  - **Logic:** Kiểm tra trùng `MasoThue` của khách hàng **trong cùng một DonviID** (cùng công ty không được trùng MST khách).
- **PUT /api/customers/{id}** (UpdateCustomerCommand)
  - **Logic:** Cập nhật thông tin (Tên, Email nhận hóa đơn, Địa chỉ).
- **DELETE /api/customers/{id}** (DeleteCustomerCommand)
  - **Logic:** **Ràng buộc:** Kiểm tra nếu `TTkhachhang.ID` đã tồn tại trong bảng `TTHoadon` (nghĩa là đã xuất hóa đơn cho khách này) thì ném lỗi `Exception`, từ chối xóa [4]. Nếu chưa có, thực hiện Soft Delete (`Is_Deleted = true`).

### UC08: Quản lý Danh mục hàng hóa (Danhmuchanghoa)
*Mô tả: Quản lý sản phẩm, dịch vụ để lập hóa đơn.*
- **GET /api/companies/{donviId}/products** (GetProductsByCompanyQuery)
- **POST /api/products** (CreateProductCommand)
  - **Logic:** Kiểm tra trùng `Mã hàng hóa` hoặc `Tenhanghoa` trong cùng một `DonviID` [5].
- **PUT /api/products/{id}** (UpdateProductCommand)
- **DELETE /api/products/{id}** (DeleteProductCommand)
  - **Logic:** **Ràng buộc:** Nếu hàng hóa đã được sử dụng trong bảng `TTHangHoa` (chi tiết hóa đơn), hệ thống chặn thao tác xóa và chỉ cho phép cập nhật trạng thái sang "Ngưng sử dụng" [5].

## 4. Ràng buộc Validation (Dùng FluentValidation)
- **TTcty.MasoThue** & **TTkhachhang.MasoThue:** Bắt buộc nhập, độ dài chuẩn (10-14 ký tự), chỉ chứa số hoặc dấu gạch ngang [2].
- **TTkhachhang.Email:** Bắt buộc phải đúng định dạng Email để hệ thống gửi hóa đơn điện tử tự động [2].
- **Danhmuchanghoa.Dongia:** Phải lớn hơn hoặc bằng 0.

## 5. Hướng dẫn sinh Code cho Cursor (Cursor Instructions)
Khi đọc file này, hãy thực hiện theo trình tự sau:
1. **Domain:** Tạo các Entities `TTcty`, `TTkhachhang`, `Danhmuchanghoa`.
2. **Application:** 
   - Tạo các thư mục tương ứng trong `Features/`.
   - Triển khai CQRS với `MediatR` cho toàn bộ các Endpoints ở mục 3.
   - Viết các rule `FluentValidation` như mục 4.
   - **Chú ý:** Xử lý triệt để logic chặn xóa/cập nhật dựa trên quan hệ dữ liệu (kiểm tra foreign key tồn tại hay chưa).
3. **Infrastructure:** Cấu hình EntityTypeConfiguration, map các FK `DonviID`. Nhớ thêm Global Query Filter `Is_Deleted == false`.
4. **Api:** Tạo `CompaniesController`, `CustomersController`, `ProductsController`. Inject `IMediator` và map API Endpoints chuẩn RESTful.