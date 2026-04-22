namespace Zenvoyce.Domain.Entities;

public class Ttkhachhang
{
    public Guid Id { get; set; }
    public Guid Donviid { get; set; }
    public string Tenkhachhang { get; set; } = string.Empty;
    public string? Masothue { get; set; }
    public string? Email { get; set; }
    public string? Dienthoai { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
