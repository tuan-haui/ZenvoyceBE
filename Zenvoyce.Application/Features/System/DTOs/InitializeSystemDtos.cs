namespace Zenvoyce.Application.Features.System.DTOs;

public class InitializeSystemResponseDto
{
    public bool Initialized { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? AdminUserId { get; set; }
    public int RolesCreated { get; set; }
    public int MenusCreated { get; set; }
    public int AdminPermissionRows { get; set; }
}
