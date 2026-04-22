namespace Zenvoyce.Domain.Entities;

public class Mauchocty
{
    public Guid Id { get; set; }
    public Guid Maugocid { get; set; }
    public Guid Donviid { get; set; }
    public string? Css { get; set; }
    public string? Header { get; set; }
    public short Trangthaiphathanh { get; set; }
    public bool Lamaumacdinh { get; set; }
    public DateTime? Ngaykichhoat { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
