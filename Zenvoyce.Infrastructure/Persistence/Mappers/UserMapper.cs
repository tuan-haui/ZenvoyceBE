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
            Dienthoai = source.Dienthoai,
            Trangthai = source.Trangthai ?? 1,
            CreatedAt = source.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = source.UpdatedAt ?? DateTime.UtcNow,
            CreatedBy = source.CreatedBy,
            UpdatedBy = source.UpdatedBy,
            IsDeleted = source.IsDeleted ?? false
        };
    }

    public static void ApplyFromDomain(this InfraUser target, Nguoidung source)
    {
        target.Id = source.Id;
        target.Madonvi = source.Madonvi;
        target.Tendangnhap = source.Tendangnhap;
        target.Matkhau = source.Matkhau;
        target.Dienthoai = source.Dienthoai;
        target.Trangthai = source.Trangthai;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
        target.CreatedBy = source.CreatedBy;
        target.UpdatedBy = source.UpdatedBy;
        target.IsDeleted = source.IsDeleted;
    }
}
