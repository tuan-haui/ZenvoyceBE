namespace Zenvoyce.Application.Features.Menus.DTOs;

public class MenuDto
{
    public Guid Id { get; set; }
    public string Tenmenu { get; set; } = string.Empty;
    public string? Duongdan { get; set; }
    public Guid? MenuchaId { get; set; }
    public Guid? QuyenId { get; set; }
}
