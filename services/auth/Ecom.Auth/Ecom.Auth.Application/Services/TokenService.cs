using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.RoleName ?? string.Empty)
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

        var entity = new RefreshToken
        {
            UserId = userId,
            Token = token,
            Expiry = DateTime.UtcNow.AddDays(expiryDays),
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(entity);
        return entity;
    }

    // ── Rotation + Reuse Detection ────────────────────────────────────────────────
    public async Task<(string newAccessToken, string newRefreshToken)?> RotateRefreshTokenAsync(string incomingToken)
    {
        var existing = await _refreshTokenRepository.GetByTokenAsync(incomingToken);

        if (existing == null)
            return null;

        // Reuse detection: token was already revoked — nuke all sessions (stolen token scenario)
        if (existing.IsRevoked)
        {
            await RevokeAllTokensAsync(existing.UserId);
            return null;
        }

        // Check expiry - using <= instead of < (JWT standard) to treat exactly-expired tokens as invalid
        // This is intentionally more strict for security - tokens expire at the exact moment, not after
        if (existing.Expiry <= DateTime.UtcNow)
            return null;

        var user = existing.User;
        if (user == null || user.IsDeleted || !user.IsActive)
            return null;

        // Rotate — generate new pair
        var newRefreshToken = GenerateRefreshToken();
        var newAccessToken = GenerateAccessToken(user);

        // Mark old token as revoked
        existing.IsRevoked = true;

        var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");
        var newEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            Expiry = DateTime.UtcNow.AddDays(expiryDays),
            IsRevoked = false
        };

        await _refreshTokenRepository.UpdateAsync(existing);
        await _refreshTokenRepository.AddAsync(newEntity);

        return (newAccessToken, newRefreshToken);
    }

    // ── Single Token Revocation (Logout) ──────────────────────────────────────────
    public async Task<bool> RevokeTokenAsync(string token, int userId)
    {
        var entity = await _refreshTokenRepository.GetActiveTokenAsync(token, userId);

        if (entity == null)
            return false;

        entity.IsRevoked = true;

        await _refreshTokenRepository.UpdateAsync(entity);
        return true;
    }

    // ── Bulk Revocation (Logout-All) ──────────────────────────────────────────────
    public async Task RevokeAllTokensAsync(int userId)
    {
        var tokens = await _refreshTokenRepository.GetActiveTokensByUserAsync(userId);

        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(t);
        }
    }
}
