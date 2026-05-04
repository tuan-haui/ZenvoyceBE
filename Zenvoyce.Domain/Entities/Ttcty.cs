namespace Zenvoyce.Domain.Entities;

public class Ttcty
{
    public Guid Id { get; set; }
    public string Masothue { get; set; } = string.Empty;
    public string Tendonvi { get; set; } = string.Empty;
    public string? Diachi { get; set; }
    public string? Dienthoai { get; set; }
    public string? Nguoidaidien { get; set; }
    public string? Emailcongty { get; set; }
    public int? BankId { get; set; }
    public string? BankAccount { get; set; }
    public short Trangthai { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
