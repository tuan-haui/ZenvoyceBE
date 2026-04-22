using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Ttcty
{
    public Guid Id { get; set; }

    public string Masothue { get; set; } = null!;

    public string Tendonvi { get; set; } = null!;

    public string? Diachi { get; set; }

    public string? Dienthoai { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Danhmuchanghoa> Danhmuchanghoas { get; set; } = new List<Danhmuchanghoa>();

    public virtual ICollection<Mauchocty> Mauchocties { get; set; } = new List<Mauchocty>();

    public virtual ICollection<Nguoidung> Nguoidungs { get; set; } = new List<Nguoidung>();

    public virtual ICollection<Tthoadon> Tthoadons { get; set; } = new List<Tthoadon>();

    public virtual ICollection<Ttkhachhang> Ttkhachhangs { get; set; } = new List<Ttkhachhang>();
}
