using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Nguoidung
{
    public Guid Id { get; set; }

    public Guid? Madonvi { get; set; }

    public string Tendangnhap { get; set; } = null!;

    public string Matkhau { get; set; } = null!;

    public string? Dienthoai { get; set; }

    public short? Trangthai { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public string? Hoten { get; set; }

    public string? Email { get; set; }

    public Guid? Quyenid { get; set; }

    public virtual ICollection<Lichsuhoadon> Lichsuhoadons { get; set; } = new List<Lichsuhoadon>();

    public virtual Ttcty? MadonviNavigation { get; set; }

    public virtual ICollection<Phanquyenchucnang> Phanquyenchucnangs { get; set; } = new List<Phanquyenchucnang>();

    public virtual Nhomquyen? Quyen { get; set; }
}
