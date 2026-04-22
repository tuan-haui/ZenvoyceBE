namespace Zenvoyce.Application.Features.Companies.DTOs;

public class CompanyDto
{
    public Guid Id { get; set; }
    public string Masothue { get; set; } = string.Empty;
    public string Tendonvi { get; set; } = string.Empty;
    public string? Diachi { get; set; }
    public string? Dienthoai { get; set; }
    public short Trangthai { get; set; }
}
