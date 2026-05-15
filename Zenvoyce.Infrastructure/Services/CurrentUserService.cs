using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Zenvoyce.Domain.Constants;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => ParseGuid(Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

    public Guid? RoleId => ParseGuid(Principal?.FindFirst(JwtClaimTypes.RoleId)?.Value);

    public string? RoleName => Principal?.FindFirst(JwtClaimTypes.RoleName)?.Value
        ?? Principal?.FindFirst(ClaimTypes.Role)?.Value;

    public Guid? CompanyId => ParseGuid(Principal?.FindFirst(JwtClaimTypes.CompanyId)?.Value);

    public string? Username => Principal?.FindFirst(JwtClaimTypes.Username)?.Value
        ?? Principal?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

    public string? FullName => Principal?.FindFirst(JwtRegisteredClaimNames.Name)?.Value
        ?? Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value;

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;
}
