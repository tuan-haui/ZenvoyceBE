using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Tthanghoa
{
    public Guid Id { get; set; }

    public Guid? Hoadonid { get; set; }

    public Guid? Hanghoaid { get; set; }

    public decimal? Soluong { get; set; }

    public decimal? Dongia { get; set; }

    public decimal? Thuesuat { get; set; }

    public decimal? Thanhtien { get; set; }

    public virtual Danhmuchanghoa? Hanghoa { get; set; }

    public virtual Tthoadon? Hoadon { get; set; }
}
