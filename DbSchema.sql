-- =======================================================================
-- KHỐI LỆNH RESET (Tùy chọn): Bỏ comment khối này nếu bạn muốn xóa sạch 
-- các bảng cũ để khởi tạo lại toàn bộ cấu trúc.
-- =======================================================================
/*
DROP TABLE IF EXISTS QLKyso, Lichsuguithue, LichsuHoadon CASCADE;
DROP TABLE IF EXISTS TTHangHoa, TTHoadon CASCADE;
DROP TABLE IF EXISTS ThongtinHDMau, Mauchocty, MauhoadonGoc CASCADE;
DROP TABLE IF EXISTS PhanQuyenChucNang, Sysmenu, Nhomquyen, Nguoidung CASCADE;
DROP TABLE IF EXISTS Danhmuchanghoa, TTkhachhang, TTcty CASCADE;
*/

-- =======================================================================
-- 1. PHÂN HỆ QUẢN LÝ DANH MỤC 
-- =======================================================================

CREATE TABLE IF NOT EXISTS TTcty (
    ID UUID PRIMARY KEY,
    MasoThue VARCHAR(20) UNIQUE NOT NULL,
    Tendonvi VARCHAR(255) NOT NULL,
    Diachi VARCHAR(500),
    Dienthoai VARCHAR(20),
    Created_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Updated_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Created_By UUID,
    Updated_By UUID,
    Is_Deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS TTkhachhang (
    ID UUID PRIMARY KEY,
    DonviID UUID REFERENCES TTcty(ID) ON DELETE CASCADE,
    Tenkhachhang VARCHAR(255) NOT NULL,
    MasoThue VARCHAR(20),
    Email VARCHAR(100),
    Dienthoai VARCHAR(20),
    Created_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Updated_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Created_By UUID,
    Updated_By UUID,
    Is_Deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS Danhmuchanghoa (
    ID UUID PRIMARY KEY,
    DonviID UUID REFERENCES TTcty(ID) ON DELETE CASCADE,
    Tenhanghoa VARCHAR(255) NOT NULL,
    Donvitinh VARCHAR(50),
    Dongia DECIMAL(18,2),
    Created_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Updated_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Created_By UUID,
    Updated_By UUID,
    Is_Deleted BOOLEAN DEFAULT FALSE
);


-- =======================================================================
-- 2. PHÂN HỆ HỆ THỐNG VÀ NGƯỜI DÙNG
-- =======================================================================

CREATE TABLE IF NOT EXISTS Nhomquyen (
    ID UUID PRIMARY KEY,
    Tenquyen VARCHAR(100) NOT NULL,
    Mota VARCHAR(255),
    Created_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Updated_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Created_By UUID,
    Updated_By UUID,
    Is_Deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS Sysmenu (
    ID UUID PRIMARY KEY,
    Tenmenu VARCHAR(100) NOT NULL,
    Duongdan VARCHAR(255),
    MenuchaID UUID REFERENCES Sysmenu(ID) ON DELETE CASCADE
);

-- N-N: Nhóm quyền <-> Menu (cấu trúc mới theo phanquyen.md)
CREATE TABLE IF NOT EXISTS SysGroupMenu (
    ID UUID PRIMARY KEY,
    Quyenid UUID NOT NULL REFERENCES Nhomquyen(ID) ON DELETE CASCADE,
    Menuid UUID NOT NULL REFERENCES Sysmenu(ID) ON DELETE CASCADE,
    Created_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Created_By UUID,
    UNIQUE (Quyenid, Menuid)
);

CREATE TABLE IF NOT EXISTS Nguoidung (
    ID UUID PRIMARY KEY,
    Madonvi UUID REFERENCES TTcty(ID) ON DELETE SET NULL,
    Quyenid UUID REFERENCES Nhomquyen(ID) ON DELETE RESTRICT,
    Tendangnhap VARCHAR(50) UNIQUE NOT NULL,
    Matkhau VARCHAR(255) NOT NULL,
    Dienthoai VARCHAR(20),
    Trangthai SMALLINT DEFAULT 1,
    Created_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Updated_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Created_By UUID,
    Updated_By UUID,
    Is_Deleted BOOLEAN DEFAULT FALSE
);

-- Legacy: không dùng trong code mới; giữ bảng để không mất dữ liệu cũ.
CREATE TABLE IF NOT EXISTS PhanQuyenChucNang (
    NguoidungID UUID REFERENCES Nguoidung(ID) ON DELETE CASCADE,
    QuyenID UUID REFERENCES Nhomquyen(ID) ON DELETE CASCADE,
    MenuID UUID REFERENCES Sysmenu(ID) ON DELETE CASCADE,
    PRIMARY KEY (NguoidungID, QuyenID, MenuID)
);


-- =======================================================================
-- 3. PHÂN HỆ QUẢN LÝ MẪU HÓA ĐƠN
-- =======================================================================

CREATE TABLE IF NOT EXISTS MauhoadonGoc (
    ID UUID PRIMARY KEY,
    Tenmau VARCHAR(255) NOT NULL,
    Loaihoadon VARCHAR(100),
    Kyhieu VARCHAR(50) UNIQUE,
    CautrucXML TEXT,
    Created_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Updated_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Created_By UUID,
    Updated_By UUID,
    Is_Deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS Mauchocty (
    ID UUID PRIMARY KEY,
    MaugocID UUID REFERENCES MauhoadonGoc(ID) ON DELETE CASCADE,
    DonviID UUID REFERENCES TTcty(ID) ON DELETE CASCADE,
    CSS TEXT,
    Header TEXT,
    TrangthaiPhatHanh SMALLINT DEFAULT 0,
    LaMauMacDinh BOOLEAN DEFAULT FALSE,
    NgayKichHoat TIMESTAMP,
    Created_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Updated_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Created_By UUID,
    Updated_By UUID,
    Is_Deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS ThongtinHDMau (
    ID UUID PRIMARY KEY,
    MauctyID UUID REFERENCES Mauchocty(ID) ON DELETE CASCADE,
    Tentruong VARCHAR(100),
    Vitrinam VARCHAR(50),
    Font VARCHAR(50),
    Canle VARCHAR(20)
);


-- =======================================================================
-- 4. PHÂN HỆ NGHIỆP VỤ HÓA ĐƠN
-- =======================================================================

CREATE TABLE IF NOT EXISTS TTHoadon (
    ID UUID PRIMARY KEY,
    DonviID UUID REFERENCES TTcty(ID) ON DELETE CASCADE,
    KhachhangID UUID REFERENCES TTkhachhang(ID) ON DELETE SET NULL,
    MauctyID UUID REFERENCES Mauchocty(ID) ON DELETE SET NULL,
    Kyhieu VARCHAR(50),
    SoHoadon VARCHAR(50),
    Ngaylap TIMESTAMP,
    Tongtien DECIMAL(18,2),
    TienThue DECIMAL(18,2),
    TongThanhToan DECIMAL(18,2),
    Trangthai VARCHAR(50),
    XMLDaKy TEXT,
    Created_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Updated_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Created_By UUID,
    Updated_By UUID,
    Is_Deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS TTHangHoa (
    ID UUID PRIMARY KEY,
    HoadonID UUID REFERENCES TTHoadon(ID) ON DELETE CASCADE,
    HanghoaID UUID REFERENCES Danhmuchanghoa(ID) ON DELETE SET NULL,
    Soluong DECIMAL(10,2),
    Dongia DECIMAL(18,2),
    ThueSuat DECIMAL(5,2),
    Thanhtien DECIMAL(18,2)
);


-- =======================================================================
-- 5. PHÂN HỆ LỊCH SỬ VÀ TÍCH HỢP THUẾ
-- =======================================================================

CREATE TABLE IF NOT EXISTS LichsuHoadon (
    ID UUID PRIMARY KEY,
    HoadonID UUID REFERENCES TTHoadon(ID) ON DELETE CASCADE,
    Hanhdong VARCHAR(255),
    TrangthaiCu VARCHAR(50),
    TrangthaiMoi VARCHAR(50),
    Thoigian TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    NguoidungID UUID REFERENCES Nguoidung(ID) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS Lichsuguithue (
    ID UUID PRIMARY KEY,
    HoadonID UUID REFERENCES TTHoadon(ID) ON DELETE CASCADE,
    Ngaygui TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    MaPhanHoi VARCHAR(50),
    NoidungPhanHoi TEXT
);

CREATE TABLE IF NOT EXISTS QLKyso (
    ID UUID PRIMARY KEY,
    HoadonID UUID REFERENCES TTHoadon(ID) ON DELETE CASCADE,
    Ngayky TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Nguoiky VARCHAR(255),
    Thongtinchungchi TEXT
);