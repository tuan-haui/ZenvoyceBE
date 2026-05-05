using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Nhomquyen
{
    public Guid Id { get; set; }

    public string Tenquyen { get; set; } = null!;

    public string? Mota { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Nguoidung> Nguoidungs { get; set; } = new List<Nguoidung>();

    public virtual ICollection<Phanquyenchucnang> Phanquyenchucnangs { get; set; } = new List<Phanquyenchucnang>();

    public virtual ICollection<Sysgroupmenu> Sysgroupmenus { get; set; } = new List<Sysgroupmenu>();
}
