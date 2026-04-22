namespace Zenvoyce.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    IReadOnlyCollection<Guid> RoleIds { get; }
}
