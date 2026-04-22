using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Lichsuguithue
{
    public Guid Id { get; set; }

    public Guid? Hoadonid { get; set; }

    public DateTime? Ngaygui { get; set; }

    public string? Maphanhoi { get; set; }

    public string? Noidungphanhoi { get; set; }

    public virtual Tthoadon? Hoadon { get; set; }
}
