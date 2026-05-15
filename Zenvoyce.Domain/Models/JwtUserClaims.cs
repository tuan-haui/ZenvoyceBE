namespace Zenvoyce.Domain.Models;

/// <summary>Thông tin người dùng đưa vào JWT khi phát hành token.</summary>
public sealed record JwtUserClaims(
    Guid UserId,
    Guid? RoleId,
    string? RoleName,
    Guid? CompanyId,
    string Username,
    string? FullName,
    string? Email);
