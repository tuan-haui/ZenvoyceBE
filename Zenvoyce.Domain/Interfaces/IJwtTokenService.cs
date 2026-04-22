namespace Zenvoyce.Domain.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, IEnumerable<Guid> roleIds, DateTime expiresAtUtc);
}
