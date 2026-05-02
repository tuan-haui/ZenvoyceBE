using Zenvoyce.Application.Features.Users.DTOs;

namespace Zenvoyce.Application.Features.Auth.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiredAt { get; set; }
    public LoginUserInfoDto UserInfo { get; set; } = new();
}

public class SeedFirstAdminResponseDto
{
    public bool Seeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
}
