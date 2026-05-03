using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecom.Auth.Application.Common;
using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Ecom.Auth.Application.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public TokenService(IConfiguration config, IRefreshTokenRepository refreshTokenRepository)
    {
        _config = config;
        _refreshTokenRepository = refreshTokenRepository;
    }

    // ── Access Token ─────────────────────────────────────────────────────────────
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.RoleName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Refresh Token Generation ──────────────────────────────────────────────────
    public string GenerateRefreshToken()
    {
        // Cryptographically random, URL-safe token
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                      .Replace("+", "-").Replace("/", "_").TrimEnd('=')
               + Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                      .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    // ── Save on Login ─────────────────────────────────────────────────────────────
    public async Task<RefreshToken> SaveRefreshTokenAsync(int userId, string token)
    {
        var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        // 🔥 SECURITY: Hash token before storing
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = TokenHasher.Hash(token),
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            IsRevoked = false,
            // Device info will be set by caller (AuthService has HttpContext access)
        };

        await _refreshTokenRepository.AddAsync(entity);
        return entity;
    }

    // ── Rotation + Reuse Detection ────────────────────────────────────────────────
    public async Task<(string newAccessToken, string newRefreshToken)?> RotateRefreshTokenAsync(string incomingToken)
    {
        // 🔥 SECURITY: Hash incoming token to lookup in database
        var tokenHash = TokenHasher.Hash(incomingToken);
        var existing = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (existing == null)
            return null;

        // 🔥 CRITICAL: Reuse detection - if token was already revoked AND has a replacement
        // This indicates a potential token theft/replay attack
        if (existing.IsRevoked && !string.IsNullOrEmpty(existing.ReplacedByTokenHash))
        {
            // Token reuse detected - revoke ALL user sessions for security
            await RevokeAllTokensAsync(existing.UserId);
            return null;
        }

        // Token already revoked but no replacement (manual revocation via logout)
        if (existing.IsRevoked)
            return null;

        // Check expiry - using <= instead of < (JWT standard) to treat exactly-expired tokens as invalid
        // This is intentionally more strict for security - tokens expire at the exact moment, not after
        if (existing.ExpiresAt <= DateTime.UtcNow)
            return null;

        var user = existing.User;
        if (user == null || user.IsDeleted || !user.IsActive)
            return null;

        // Rotate — generate new pair
        var newRefreshToken = GenerateRefreshToken();
        var newAccessToken = GenerateAccessToken(user);

        var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");
        
        // 🔥 SECURITY: Hash new token before storing
        var newEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.Hash(newRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            IsRevoked = false
        };

        // 🔥 Mark old token as revoked with audit trail
        existing.IsRevoked = true;
        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByTokenHash = TokenHasher.Hash(newRefreshToken); // 🔥 SECURITY: Store hash

        await _refreshTokenRepository.UpdateAsync(existing);
        await _refreshTokenRepository.AddAsync(newEntity);

        // 🔥 CRITICAL: Return PLAIN token to client (never return hash!)
        return (newAccessToken, newRefreshToken);
    }

    // ── Single Token Revocation (Logout) ──────────────────────────────────────────
    public async Task<bool> RevokeTokenAsync(string token, int userId)
    {
        // 🔥 SECURITY: Hash token to lookup in database
        var tokenHash = TokenHasher.Hash(token);
        var entity = await _refreshTokenRepository.GetActiveTokenAsync(tokenHash, userId);

        if (entity == null)
            return false;

        entity.IsRevoked = true;
        entity.RevokedAt = DateTime.UtcNow;

        await _refreshTokenRepository.UpdateAsync(entity);
        return true;
    }

    // ── Bulk Revocation (Logout-All) ──────────────────────────────────────────────
    public async Task RevokeAllTokensAsync(int userId)
    {
        var tokens = await _refreshTokenRepository.GetAllByUserIdAsync(userId);

        foreach (var t in tokens.Where(t => !t.IsRevoked))
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;
        }

        await _refreshTokenRepository.UpdateRangeAsync(tokens);
    }
}
