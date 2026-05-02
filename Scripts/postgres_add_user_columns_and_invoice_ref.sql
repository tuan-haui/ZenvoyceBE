-- Chạy thủ công trên PostgreSQL nếu bảng đã tồn tại.
ALTER TABLE nguoidung ADD COLUMN IF NOT EXISTS hoten character varying(200);
ALTER TABLE nguoidung ADD COLUMN IF NOT EXISTS email character varying(255);
CREATE UNIQUE INDEX IF NOT EXISTS ix_nguoidung_email_notnull
  ON nguoidung (lower(email))
  WHERE email IS NOT NULL AND is_deleted IS NOT TRUE;

ALTER TABLE tthoadon ADD COLUMN IF NOT EXISTS thamchieu_hoadon_id uuid;
-- Tham chiếu hóa đơn gốc (điều chỉnh / thay thế) — không bắt buộc FK tránh vòng tham chiếu khi cần.
