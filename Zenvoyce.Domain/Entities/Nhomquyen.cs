namespace Zenvoyce.Domain.Entities;

public class Nhomquyen
{
    public Guid Id { get; set; }
    public string Tenquyen { get; set; } = string.Empty;
    public string? Mota { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
