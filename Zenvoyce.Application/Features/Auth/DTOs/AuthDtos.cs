using Zenvoyce.Application.Features.Users.DTOs;

namespace Zenvoyce.Application.Features.Auth.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiredAt { get; set; }
    public LoginUserInfoDto UserInfo { get; set; } = new();
}

