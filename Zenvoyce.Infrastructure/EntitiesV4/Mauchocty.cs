using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Mauchocty
{
    public Guid Id { get; set; }

    public Guid? Maugocid { get; set; }

    public Guid? Donviid { get; set; }

    public string? Css { get; set; }

    public string? Header { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public short? Trangthaiphathanh { get; set; }

    /// <summary>
    /// Là mẫu mặc định?
    /// </summary>
    public bool? Lamaumacdinh { get; set; }

    public DateTimeOffset? Ngaykichhoat { get; set; }

    public virtual Ttcty? Donvi { get; set; }

    public virtual Mauhoadongoc? Maugoc { get; set; }

    public virtual ICollection<Thongtinhdmau> Thongtinhdmaus { get; set; } = new List<Thongtinhdmau>();

    public virtual ICollection<Tthoadon> Tthoadons { get; set; } = new List<Tthoadon>();
}
