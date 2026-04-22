# Cấu hình phát triển API Backend - Phân hệ 4: Nghiệp vụ Hóa đơn

## 1. Tổng quan kỹ thuật (Technical Stack)
- **Framework & Kiến trúc:** .NET 9, Clean Architecture, CQRS (MediatR), AutoMapper.
- **Cơ sở dữ liệu:** PostgreSQL với EF Core 9.
- **Transaction (Cực kỳ quan trọng):** Các nghiệp vụ Master-Detail (như lưu Hóa đơn và Chi tiết hàng hóa) phải được thực hiện trong cùng một Database Transaction.
- **Xử lý ID:** Bắt buộc khởi tạo `Guid` từ tầng Application trước khi Insert để tối ưu hóa việc lưu bảng Cha - Con cùng lúc.
- **Lưu vết:** Mọi thay đổi trạng thái hóa đơn phải được log lại.

## 2. Thực thể dữ liệu tầng Domain (Domain Entities)
1. **TTHoadon (Bảng Cha):** `ID` (Guid), `DonviID` (Guid), `KhachhangID` (Guid), `MauctyID` (Guid), `Kyhieu` (string), `SoHoadon` (string - null khi chờ ký), `Ngaylap` (DateTime), `Tongtien` (decimal), `TienThue` (decimal), `TongThanhToan` (decimal), `Trangthai` (Enum: Draft, PendingSign, Issued, Adjusted, Replaced, Cancelled), `XMLDaKy` (text).
2. **TTHangHoa (Bảng Con):** `ID` (Guid), `HoadonID` (Guid - FK), `HanghoaID` (Guid - FK), `Soluong` (decimal), `Dongia` (decimal), `ThueSuat` (decimal), `Thanhtien` (decimal).
3. **LichsuHoadon (Log vòng đời):** `ID` (Guid), `HoadonID` (Guid), `TrangthaiCu` (string), `TrangthaiMoi` (string), `Hanhdong` (string), `Thoigian` (DateTime), `NguoidungID` (Guid).
4. **Lichsuguithue & QLKyso:** Log giao tiếp với Tổng cục Thuế và Log chứng thư số.

---

## 3. PHASE 1: Khởi tạo, Luân chuyển và Tra cứu (UC14, UC19, UC21, UC22)
*Mục tiêu: Xử lý các logic nội bộ trước khi đụng đến pháp lý/chữ ký số.*

### UC14: Tạo hóa đơn mới
- **POST /api/invoices** (CreateInvoiceCommand)
  - **Payload:** `{ DonviID, KhachhangID, MauctyID, Ngaylap, List<HangHoaDTO> { HanghoaID, Soluong, Dongia, ThueSuat } }`
  - **Logic & Transaction:** 
    1. Sinh `Guid` cho `TTHoadon.ID`.
    2. Duyệt danh sách hàng hóa: Tính `Thanhtien = Soluong * Dongia`. Gán `HoadonID` vừa sinh cho từng dòng `TTHangHoa`.
    3. Tính toán tổng của Hóa đơn: Tổng tiền hàng, Tổng tiền thuế, Tổng thanh toán.
    4. Set `Trangthai = Draft` (Chờ ký). Lưu `TTHoadon` và danh sách `TTHangHoa` trong cùng 1 DbContext Transaction.
    5. Ghi log vào `LichsuHoadon`.

### UC19: Gửi hóa đơn chờ ký
- **POST /api/invoices/{id}/forward** (ForwardInvoiceCommand)
  - **Logic:** Chuyển trạng thái từ `Draft` sang `PendingSign` (Gửi duyệt ký) và log lịch sử.

### UC21 & UC22: Tra cứu & Lịch sử
- **GET /api/invoices** (GetInvoicesQuery) - Hỗ trợ lọc theo Khách hàng, Trạng thái, Từ ngày, Đến ngày.
- **GET /api/invoices/{id}/history** (GetInvoiceHistoryQuery) - Truy vấn bảng `LichsuHoadon` sắp xếp theo thời gian.

---

## 4. PHASE 2: Xử lý Pháp lý và Cơ quan Thuế (UC15, UC16, UC17, UC18)
*Mục tiêu: Đóng dấu mộc điện tử và phát hành hợp pháp.*

### UC15: Ký số hóa đơn
- **POST /api/invoices/{id}/sign** (SignInvoiceCommand)
  - **Logic:** 
    1. Đảm bảo trạng thái đang là `PendingSign` hoặc `Draft`.
    2. (Mock) Sinh chuỗi XML từ dữ liệu hóa đơn và đóng dấu chữ ký điện tử. Lưu chuỗi này vào cột `XMLDaKy`.
    3. Cập nhật `Trangthai = Signed`. Ghi log vào bảng `QLKyso`.

### UC16: Phát hành hóa đơn (Gửi Thuế)
- **POST /api/invoices/{id}/publish** (PublishInvoiceCommand)
  - **Logic:**
    1. Đảm bảo hóa đơn đã có `XMLDaKy`.
    2. (Mock External API): Gọi API Tổng cục Thuế. Delay 2s.
    3. Nhận phản hồi thành công: Sinh `SoHoadon` chính thức (VD: 0000001). Cập nhật `Trangthai = Issued`.
    4. Ghi log vào `Lichsuguithue`.

### UC17 & UC18: Xử lý sai sót & Hủy
- **POST /api/invoices/{id}/cancel** (CancelInvoiceCommand): Nhập lý do, cập nhật trạng thái thành `Cancelled`. Chỉ áp dụng cho hóa đơn đã ký hoặc phát hành.
- **POST /api/invoices/{id}/adjust**: Tạo 1 hóa đơn mới (Trạng thái Draft) có trường tham chiếu (ReferenceId) trỏ về ID của hóa đơn gốc.

---

## 5. PHASE 3: Tương tác Khách hàng & Báo cáo (UC20, UC23)
*Mục tiêu: Phân phối chứng từ và thống kê.*

### UC20: Gửi hóa đơn cho khách hàng
- **POST /api/invoices/{id}/send-email** (SendInvoiceEmailCommand)
  - **Logic:** Query bảng `TTkhachhang` để lấy Email. (Mock) Gọi Notification Service gửi email chứa bản PDF/XML hóa đơn. Chỉ được gửi khi `Trangthai = Issued`.

### UC23: Xuất báo cáo tổng hợp
- **GET /api/invoices/reports/sales** (GetSalesReportQuery)
  - **Logic:** Tổng hợp doanh thu, tiền thuế theo từng Khách hàng hoặc khoảng thời gian. Trả về DTO để Frontend hiển thị biểu đồ hoặc tải Excel.

---

## 6. Hướng dẫn sinh Code cho Cursor (System Prompt)
**Ràng buộc chéo (Cross-Module Dependencies):**
- Tầng Application của Phân hệ 4 bắt buộc phải query dữ liệu từ `TTkhachhang` và `Danhmuchanghoa` (thuộc Phân hệ 2) để đối chiếu tính hợp lệ trước khi tạo Hóa đơn.
- Hãy tách các Logic phức tạp (như tính toán thành tiền, thuế) vào các phương thức private bên trong Command Handler để code Clean.
- Transaction là bắt buộc ở UC14. Hãy dùng `await _context.SaveChangesAsync(cancellationToken);` 1 lần duy nhất sau khi đã `Add` cả bảng cha và con.