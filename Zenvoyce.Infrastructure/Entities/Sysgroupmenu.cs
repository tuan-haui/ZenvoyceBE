using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Sysgroupmenu
{
    public Guid Id { get; set; }

    public Guid Quyenid { get; set; }

    public Guid Menuid { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public virtual Sysmenu Menu { get; set; } = null!;

    public virtual Nhomquyen Quyen { get; set; } = null!;
}
