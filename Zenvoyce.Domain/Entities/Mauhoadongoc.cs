namespace Zenvoyce.Domain.Entities;

public class Mauhoadongoc
{
    public Guid Id { get; set; }
    public string Tenmau { get; set; } = string.Empty;
    public string? Cautrucxml { get; set; }
    public string? Loaihoadon { get; set; }
    public string? Kyhieu { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
