namespace Zenvoyce.Domain.Entities;

public class Danhmuchanghoa
{
    public Guid Id { get; set; }
    public Guid Donviid { get; set; }
    public string Tenhanghoa { get; set; } = string.Empty;
    public string? Donvitinh { get; set; }
    public decimal Dongia { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
