using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Danhmuchanghoa
{
    public Guid Id { get; set; }

    public Guid? Donviid { get; set; }

    public string Tenhanghoa { get; set; } = null!;

    public string? Donvitinh { get; set; }

    public decimal? Dongia { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public string? Sku { get; set; }

    public short? Thuesuat { get; set; }

    public virtual Ttcty? Donvi { get; set; }

    public virtual ICollection<Tthanghoa> Tthanghoas { get; set; } = new List<Tthanghoa>();
}
