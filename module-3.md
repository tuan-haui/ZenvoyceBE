# Cấu hình phát triển API Backend - Phân hệ 3: Quản lý Mẫu hóa đơn

## 1. Tổng quan kỹ thuật (Technical Stack)
- **Framework & Kiến trúc:** .NET 9, Clean Architecture, CQRS (MediatR), AutoMapper.
- **Cơ sở dữ liệu:** PostgreSQL với EF Core 9.
- **Kiểu dữ liệu ID:** Bắt buộc sử dụng **Guid**, được khởi tạo từ tầng Application trước khi Insert để tối ưu Transaction.
- **Lưu vết & Xóa mềm:** Mọi entity kế thừa `BaseEntity` (Created_At, Updated_At, Created_By, Updated_By, Is_Deleted).

## 2. Thực thể dữ liệu tầng Domain (Domain Entities)
1. **MauhoadonGoc (Mẫu gốc):** `ID` (Guid), `Tenmau` (string), `CautrucXML` (text), `LoaiHoadon` (string), `Kyhieu` (string).
2. **Mauchocty (Mẫu áp dụng cho công ty):** `ID` (Guid), `MaugocID` (Guid - FK), `DonviID` (Guid - FK bảng TTcty), `CSS` (text), `Header` (text), `TrangthaiPhatHanh` (int: 0-Chưa phát hành, 1-Đang chờ, 2-Đã chấp nhận, 3-Từ chối), `LaMauMacDinh` (boolean), `NgayKichHoat` (DateTime).
3. **ThongtinHDMau (Metadata mẫu):** `ID` (Guid), `MauctyID` (Guid - FK), `Tentruong` (string), `Vitrinam` (string), `Font` (string), `Canle` (string).

---

## 3. PHASE 1: Quản trị Kho mẫu và Tùy biến Doanh nghiệp 
*Mục tiêu: API cho Admin tạo mẫu nền tảng và Kế toán lấy mẫu đó về tùy biến cho doanh nghiệp.*

### UC09: Thiết lập mẫu hóa đơn gốc
* **Mô tả:** Admin tạo mới các mẫu hóa đơn nền tảng (GTGT, bán hàng...) để đưa vào kho mẫu chuẩn.
* **POST /api/templates/base** (CreateBaseTemplateCommand)
  - **Payload:** `{ Tenmau, LoaiHoadon, Kyhieu, CautrucXML }`
  - **Luồng chính:** Hệ thống nhận dữ liệu, kiểm tra tính hợp lệ của cấu trúc file XML và lưu vào cơ sở dữ liệu kho mẫu gốc.
  - **Luồng ngoại lệ (Validation):** Báo lỗi BadRequest và KHÔNG cho phép lưu nếu `Kyhieu` mẫu hóa đơn bị trùng lặp hoặc file định dạng không hợp lệ.

### UC10: Chỉnh sửa mẫu hóa đơn gốc
* **Mô tả:** Admin thay đổi thiết kế giao diện PDF/XML của mẫu gốc.
* **PUT /api/templates/base/{id}** (UpdateBaseTemplateCommand)
  - **Luồng chính:** Cập nhật thông tin thiết kế (màu sắc, font chữ, bố cục hiển thị) vào DB.
  - **Luồng ngoại lệ (Business Rule):** Hệ thống phải query kiểm tra bảng `Mauchocty`. Nếu `MaugocID` này đã được áp dụng cho bất kỳ công ty nào, hệ thống phải ném ra Exception: *"Không thể chỉnh sửa mẫu đã được đưa vào sử dụng"*.

### UC11: Áp dụng mẫu cho công ty
* **Mô tả:** Kế toán trưởng gán mẫu gốc cho công ty, tùy biến logo, header và đặt làm mẫu mặc định.
* **POST /api/templates/company/apply** (ApplyTemplateCommand)
  - **Payload:** `{ MaugocID, DonviID, CSS, Header, LaMauMacDinh, List<ThongtinHDMauDTO> Metadata }`
  - **Luồng chính:** 
    1. Kế thừa mẫu gốc, lưu cấu hình tùy biến (Logo, Header) vào bảng `Mauchocty`.
    2. Bulk Insert các thông số tọa độ hiển thị vào bảng `ThongtinHDMau`.
    3. Hệ thống ghi nhận ngày kích hoạt (`NgayKichHoat = DateTime.UtcNow`) và set `TrangthaiPhatHanh = 0` (Chưa phát hành).
    4. **Transaction Rule:** Phải bọc toàn bộ trong DB Transaction. Nếu `LaMauMacDinh == true`, hệ thống phải tự động tìm các mẫu khác của cùng `DonviID` và cập nhật `LaMauMacDinh = false`.
  - **Luồng ngoại lệ:** Báo lỗi nếu ID Mẫu gốc hoặc ID Đơn vị không tồn tại.

---

## 4. PHASE 2: Đăng ký và Giao tiếp Cơ quan Thuế
*Mục tiêu: Quản lý danh sách mẫu đã tùy biến và gọi API gửi lên Tổng cục Thuế.*

### UC12: Quản lý kho mẫu phát hành
* **Mô tả:** Xem danh sách và tra cứu các mẫu hóa đơn của công ty để theo dõi trạng thái.
* **GET /api/templates/company** (GetCompanyTemplatesQuery)
  - **Query Params:** `DonviID` (bắt buộc), `KyhieuMau`, `LoaiHoadon`, `TrangthaiPhatHanh`.
  - **Luồng chính:** Trả về danh sách mẫu hóa đơn tương ứng với bộ lọc. Trả về kèm chi tiết lịch sử trạng thái của mẫu.

### UC13: Thông báo phát hành mẫu
* **Mô tả:** Gửi dữ liệu đăng ký mẫu đến Tổng cục Thuế để được cấp phép sử dụng.
* **POST /api/templates/company/{id}/notify-tax** (NotifyTaxAuthorityCommand)
  - **Luồng chính & State Machine:**
    1. Lấy `Mauchocty` theo ID. **Ràng buộc:** Chỉ thực hiện khi trạng thái hiện tại là 0 (Chưa phát hành) hoặc 3 (Từ chối).
    2. Đóng gói dữ liệu đăng ký thành file XML chuẩn (Tạm thời Mock logic tạo XML).
    3. Đổi trạng thái `TrangthaiPhatHanh = 1` (Đang chờ) và cập nhật DB.
    4. Gọi external API (Mock Delay 2s). 
    5. Nhận phản hồi: Nếu thành công -> Cập nhật `TrangthaiPhatHanh = 2` (Đã chấp nhận).
  - **Luồng ngoại lệ:** Nếu dữ liệu bị TCT từ chối do sai cấu trúc/chữ ký số, tự động chuyển `TrangthaiPhatHanh = 3` (Từ chối) và trả về mã lỗi kèm nội dung chi tiết.

---

## 5. Hướng dẫn sinh Code cho Cursor (System Prompt)
Khi lập trình phân hệ này, hãy tuân thủ chặt chẽ các "Luồng ngoại lệ" và "Transaction Rule" được định nghĩa ở trên. Việc xử lý trạng thái phát hành (State Machine) ở UC13 cần được tách thành các private method rõ ràng trong Command Handler.