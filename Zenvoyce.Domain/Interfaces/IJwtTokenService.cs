namespace Zenvoyce.Domain.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, Guid? roleId, DateTime expiresAtUtc);
}
