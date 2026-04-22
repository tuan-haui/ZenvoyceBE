using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Qlkyso
{
    public Guid Id { get; set; }

    public Guid? Hoadonid { get; set; }

    public DateTime? Ngayky { get; set; }

    public string? Nguoiky { get; set; }

    public string? Thongtinchungchi { get; set; }

    public virtual Tthoadon? Hoadon { get; set; }
}
