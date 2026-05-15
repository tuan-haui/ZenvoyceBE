using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Zenvoyce.Domain.Constants;
using Zenvoyce.Domain.Interfaces;
using Zenvoyce.Domain.Models;

namespace Zenvoyce.Infrastructure.Security;

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateToken(JwtUserClaims userClaims, DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userClaims.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, userClaims.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, userClaims.Username),
            new(JwtClaimTypes.Username, userClaims.Username)
        };

        if (!string.IsNullOrWhiteSpace(userClaims.FullName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, userClaims.FullName));
            claims.Add(new Claim(ClaimTypes.Name, userClaims.FullName));
        }

        if (!string.IsNullOrWhiteSpace(userClaims.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, userClaims.Email));
            claims.Add(new Claim(ClaimTypes.Email, userClaims.Email));
        }

        if (userClaims.CompanyId.HasValue)
        {
            claims.Add(new Claim(JwtClaimTypes.CompanyId, userClaims.CompanyId.Value.ToString()));
        }

        if (userClaims.RoleId.HasValue)
        {
            claims.Add(new Claim(JwtClaimTypes.RoleId, userClaims.RoleId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(userClaims.RoleName))
        {
            claims.Add(new Claim(JwtClaimTypes.RoleName, userClaims.RoleName));
            claims.Add(new Claim(ClaimTypes.Role, userClaims.RoleName));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
