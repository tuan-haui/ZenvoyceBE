namespace Zenvoyce.Application.Features.Customers.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }
    public Guid Donviid { get; set; }
    public string Tenkhachhang { get; set; } = string.Empty;
    public string? Masothue { get; set; }
    public string? Email { get; set; }
    public string? Dienthoai { get; set; }
}
