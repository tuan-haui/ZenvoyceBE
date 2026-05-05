using Zenvoyce.Domain.Entities;
using InfraUser = Zenvoyce.Infrastructure.Entities.Nguoidung;

namespace Zenvoyce.Infrastructure.Persistence.Mappers;

internal static class UserMapper
{
    public static Nguoidung ToDomain(this InfraUser source)
    {
        return new Nguoidung
        {
            Id = source.Id,
            Madonvi = source.Madonvi,
            Tendangnhap = source.Tendangnhap,
            Matkhau = source.Matkhau,
            Hoten = source.Hoten,
            Email = source.Email,
            Dienthoai = source.Dienthoai,
            Trangthai = source.Trangthai ?? 1,
            CreatedAt = source.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = source.UpdatedAt ?? DateTime.UtcNow,
            CreatedBy = source.CreatedBy,
            UpdatedBy = source.UpdatedBy,
            IsDeleted = source.IsDeleted ?? false,
            Quyenid = source.Quyenid,
            Tenquyen = source.Quyen?.Tenquyen
        };
    }

    public static void ApplyFromDomain(this InfraUser target, Nguoidung source)
    {
        target.Id = source.Id;
        target.Madonvi = source.Madonvi;
        target.Tendangnhap = source.Tendangnhap;
        target.Matkhau = source.Matkhau;
        target.Hoten = source.Hoten;
        target.Email = source.Email;
        target.Dienthoai = source.Dienthoai;
        target.Trangthai = source.Trangthai;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
        target.CreatedBy = source.CreatedBy;
        target.UpdatedBy = source.UpdatedBy;
        target.IsDeleted = source.IsDeleted;
        target.Quyenid = source.Quyenid;
    }
}
