using Ecom.Auth.Domain.Entities;

namespace Ecom.Auth.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();

    /// <summary>
    /// Stores a new refresh token in DB after login.
    /// </summary>
    Task<RefreshToken> SaveRefreshTokenAsync(int userId, string token);

    /// <summary>
    /// Rotates the refresh token: revokes old, issues new.
    /// Returns null if invalid/expired. Revokes ALL user tokens if a revoked token is reused.
    /// </summary>
    Task<(string newAccessToken, string newRefreshToken)?> RotateRefreshTokenAsync(string incomingToken);

    /// <summary>
    /// Revokes a single refresh token on logout.
    /// </summary>
    Task<bool> RevokeTokenAsync(string token, int userId);

    /// <summary>
    /// Revokes all active refresh tokens for a user on logout-all.
    /// </summary>
    Task RevokeAllTokensAsync(int userId);
}
