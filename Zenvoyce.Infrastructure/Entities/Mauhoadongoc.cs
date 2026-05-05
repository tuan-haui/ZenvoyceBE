using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Mauhoadongoc
{
    public Guid Id { get; set; }

    public string Tenmau { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public string? Loaihoadon { get; set; }

    public string Kyhieu { get; set; } = null!;

    public string? HtmlContent { get; set; }

    public string? CssContent { get; set; }

    public string? Version { get; set; }

    public virtual ICollection<Mauchocty> Mauchocties { get; set; } = new List<Mauchocty>();
}
