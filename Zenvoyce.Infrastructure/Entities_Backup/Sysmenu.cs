using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Sysmenu
{
    public Guid Id { get; set; }

    public string Tenmenu { get; set; } = null!;

    public string? Duongdan { get; set; }

    public Guid? Menuchaid { get; set; }

    public Guid? Quyenid { get; set; }

    public virtual ICollection<Sysmenu> InverseMenucha { get; set; } = new List<Sysmenu>();

    public virtual Sysmenu? Menucha { get; set; }

    public virtual ICollection<Phanquyenchucnang> Phanquyenchucnangs { get; set; } = new List<Phanquyenchucnang>();

    public virtual Nhomquyen? Quyen { get; set; }
}
