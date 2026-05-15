using Zenvoyce.Domain.Models;

namespace Zenvoyce.Domain.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(JwtUserClaims userClaims, DateTime expiresAtUtc);
}
