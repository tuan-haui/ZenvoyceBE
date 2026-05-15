namespace Zenvoyce.Domain.Constants;

/// <summary>Tên claim dùng trong JWT (đồng bộ BE/FE).</summary>
public static class JwtClaimTypes
{
    public const string CompanyId = "company_id";
    public const string RoleId = "role_id";
    public const string RoleName = "role_name";
    public const string Username = "username";
}
