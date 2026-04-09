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

    public Task<RefreshToken?> GetByTokenAsync(string token) =>
        _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

    public Task<RefreshToken?> GetActiveTokenAsync(string token, int userId) =>
        _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.UserId == userId && !t.IsRevoked);

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId) =>
        await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync();
}
