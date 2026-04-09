using Ecom.Auth.Domain.Entities;

namespace Ecom.Auth.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken?> GetActiveTokenAsync(string token, int userId);
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId);
}
