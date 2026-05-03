using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.Domain.Entities;
using Ecom.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Auth.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    public RefreshTokenRepository(AuthDbContext context) => _context = context;

    public async Task AddAsync(RefreshToken token)
    {
        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(RefreshToken token)
    {
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<RefreshToken> tokens)
    {
        _context.RefreshTokens.UpdateRange(tokens);
        await _context.SaveChangesAsync();
    }

    // 🔥 SECURITY: Now accepts token hash instead of plain token
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash) =>
        _context.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    // 🔥 SECURITY: Now accepts token hash instead of plain token
    public Task<RefreshToken?> GetActiveTokenAsync(string tokenHash, int userId) =>
        _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UserId == userId && !t.IsRevoked);

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId) =>
        await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync();

    public async Task<List<RefreshToken>> GetAllByUserIdAsync(int userId) =>
        await _context.RefreshTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();
}
