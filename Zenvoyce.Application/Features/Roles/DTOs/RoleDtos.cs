namespace Zenvoyce.Application.Features.Roles.DTOs;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Tenquyen { get; set; } = string.Empty;
    public string? Mota { get; set; }
}

public class AssignPermissionsRequestDto
{
    public Guid RoleId { get; set; }
    public Guid UserId { get; set; }
    public IReadOnlyCollection<Guid> MenuIds { get; set; } = [];
}
