using Ecom.Auth.Domain.Entities;

namespace Ecom.Auth.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task UpdateRangeAsync(IEnumerable<RefreshToken> tokens);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash); // 🔥 CHANGED: Now accepts hash
    Task<RefreshToken?> GetActiveTokenAsync(string tokenHash, int userId); // 🔥 CHANGED: Now accepts hash
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId);
    Task<List<RefreshToken>> GetAllByUserIdAsync(int userId);
}
