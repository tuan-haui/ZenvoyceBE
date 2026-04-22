using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plainText)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainText);
    }

    public bool Verify(string plainText, string hashedText)
    {
        return BCrypt.Net.BCrypt.Verify(plainText, hashedText);
    }
}
