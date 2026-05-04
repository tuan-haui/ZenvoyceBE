namespace Zenvoyce.Application.Features.Templates.DTOs;

public class BaseTemplateDto
{
    public Guid Id { get; set; }
    public string Tenmau { get; set; } = string.Empty;
    public string? Loaihoadon { get; set; }
    public string? Kyhieu { get; set; }
    public string? Cautrucxml { get; set; }
}

public class TemplateMetadataDto
{
    public string? Tentruong { get; set; }
    public string? Vitrinam { get; set; }
    public string? Font { get; set; }
    public string? Canle { get; set; }
}

public class CompanyTemplateDto
{
    public Guid Id { get; set; }
    public Guid Maugocid { get; set; }
    public Guid Donviid { get; set; }
    public string? Tenmaugoc { get; set; }
    public string? Kyhieu { get; set; }
    public string? Loaihoadon { get; set; }
    public string? Css { get; set; }
    public string? Header { get; set; }
    public short Trangthaiphathanh { get; set; }
    public bool Lamaumacdinh { get; set; }
    public DateTime? Ngaykichhoat { get; set; }
    public IReadOnlyCollection<TemplateMetadataDto> Metadata { get; set; } = Array.Empty<TemplateMetadataDto>();
    public IReadOnlyCollection<TemplateStatusHistoryDto> LichsuTrangthai { get; set; } = Array.Empty<TemplateStatusHistoryDto>();
}

public class TemplateStatusHistoryDto
{
    public short Trangthai { get; set; }
    public DateTime Thoigian { get; set; }
    public string? Ghichu { get; set; }
}
