using System.Security.Cryptography;
using System.Text;

namespace Ecom.Auth.Application.Common;

/// <summary>
/// Provides secure hashing for refresh tokens.
/// Tokens are hashed before storage to prevent compromise if database is leaked.
/// </summary>
public static class TokenHasher
{
    /// <summary>
    /// Hashes a token using SHA256.
    /// </summary>
    /// <param name="token">Plain text token to hash</param>
    /// <returns>Base64-encoded hash of the token</returns>
    public static string Hash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
