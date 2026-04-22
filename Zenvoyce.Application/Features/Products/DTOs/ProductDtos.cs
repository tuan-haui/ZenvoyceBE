namespace Zenvoyce.Application.Features.Products.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public Guid Donviid { get; set; }
    public string Tenhanghoa { get; set; } = string.Empty;
    public string? Donvitinh { get; set; }
    public decimal Dongia { get; set; }
}
