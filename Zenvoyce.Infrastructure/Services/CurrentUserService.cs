using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var parsed) ? parsed : null;
        }
    }

    public Guid? RoleId
    {
        get
        {
            var first = httpContextAccessor.HttpContext?.User.FindFirst("role_id")?.Value;
            return Guid.TryParse(first, out var parsed) ? parsed : null;
        }
    }
}
