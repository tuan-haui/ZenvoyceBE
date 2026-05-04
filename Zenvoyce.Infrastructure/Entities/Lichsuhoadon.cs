using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Lichsuhoadon
{
    public Guid Id { get; set; }

    public Guid? Hoadonid { get; set; }

    public string? Hanhdong { get; set; }

    public DateTime? Thoigian { get; set; }

    public Guid? Nguoidungid { get; set; }

    public string? Trangthaicu { get; set; }

    public string? Trangthaimoi { get; set; }

    public string? Chitiethanhdong { get; set; }

    public virtual Tthoadon? Hoadon { get; set; }

    public virtual Nguoidung? Nguoidung { get; set; }
}
