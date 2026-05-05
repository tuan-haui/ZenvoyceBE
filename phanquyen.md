---

## ĐẶC TẢ CẤU TRÚC PHÂN QUYỀN HỆ THỐNG

### 1. Mối quan hệ Người dùng - Nhóm quyền

- **Kiểu quan hệ:** 1-N (Một - Nhiều)
- **Mô tả:**
  - Mỗi **người dùng** chỉ thuộc **một nhóm quyền duy nhất** tại một thời điểm.
  - Một **nhóm quyền** có thể chứa **nhiều người dùng**.

- **Cách triển khai:**
  - Thông qua trường: `nguoidung.quyenid`
  - Trường này đóng vai trò là **khóa ngoại (Foreign Key)** liên kết đến bảng nhóm quyền.

---

### 2. Mối quan hệ Nhóm quyền - Menu chức năng

- **Kiểu quan hệ:** N-N (Nhiều - Nhiều)
- **Mô tả:**
  - Một **nhóm quyền** có thể truy cập **nhiều menu chức năng**.
  - Một **menu chức năng** có thể được sử dụng bởi **nhiều nhóm quyền khác nhau**.

- **Cách triển khai:**
  - Thông qua bảng trung gian: `SysGroupMenu`

- **Vai trò của bảng `SysGroupMenu`:**
  - Liên kết giữa:
    - `GroupId` (Nhóm quyền)
    - `MenuId` (Menu chức năng)
  - Cho phép cấu hình linh hoạt quyền truy cập hệ thống.

---

### 3. Tóm tắt cấu trúc

| Thành phần       | Quan hệ | Mô tả |
|----------------|--------|------|
| Người dùng → Nhóm quyền | 1-N | Mỗi user thuộc 1 nhóm quyền |
| Nhóm quyền → Menu       | N-N | Thông qua bảng `SysGroupMenu` |

---