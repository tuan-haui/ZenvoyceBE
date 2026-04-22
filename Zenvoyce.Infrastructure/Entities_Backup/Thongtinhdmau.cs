using System;
using System.Collections.Generic;

namespace Zenvoyce.Infrastructure.Entities;

public partial class Thongtinhdmau
{
    public Guid Id { get; set; }

    public Guid? Mauctyid { get; set; }

    public string? Tentruong { get; set; }

    public string? Vitrinam { get; set; }

    public string? Font { get; set; }

    public string? Canle { get; set; }

    public virtual Mauchocty? Maucty { get; set; }
}
