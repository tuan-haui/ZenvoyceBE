using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Phanquyenchucnang
{
    public Guid Nguoidungid { get; set; }

    public Guid Quyenid { get; set; }

    public Guid Menuid { get; set; }

    public virtual Sysmenu Menu { get; set; } = null!;

    public virtual Nguoidung Nguoidung { get; set; } = null!;

    public virtual Nhomquyen Quyen { get; set; } = null!;
}
