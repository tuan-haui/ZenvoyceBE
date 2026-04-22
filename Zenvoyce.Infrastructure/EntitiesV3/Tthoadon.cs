using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Tthoadon
{
    public Guid Id { get; set; }

    public Guid? Donviid { get; set; }

    public Guid? Khachhangid { get; set; }

    public Guid? Mauctyid { get; set; }

    public string? Kyhieu { get; set; }

    public string? Sohoadon { get; set; }

    public DateTime? Ngaylap { get; set; }

    public decimal? Tongtien { get; set; }

    public decimal? Tienthue { get; set; }

    public decimal? Tongthanhtoan { get; set; }

    public string? Trangthai { get; set; }

    public string? Xmldaky { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Ttcty? Donvi { get; set; }

    public virtual Ttkhachhang? Khachhang { get; set; }

    public virtual ICollection<Lichsuguithue> Lichsuguithues { get; set; } = new List<Lichsuguithue>();

    public virtual ICollection<Lichsuhoadon> Lichsuhoadons { get; set; } = new List<Lichsuhoadon>();

    public virtual ICollection<Qlkyso> Qlkysos { get; set; } = new List<Qlkyso>();

    public virtual ICollection<Tthanghoa> Tthanghoas { get; set; } = new List<Tthanghoa>();
}
