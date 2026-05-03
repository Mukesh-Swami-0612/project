using Ecom.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ecom.Auth.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task AddAsync(User user, bool saveChanges = true);
    Task UpdateAsync(User user, bool saveChanges = true);

    // Email verification tokens
    Task AddEmailVerificationTokenAsync(EmailVerificationToken token, bool saveChanges = true);
    Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(string token);

    // Role validation
    Task<bool> RoleExistsAsync(int roleId);

    // Transaction support
    Task<IDbContextTransaction> BeginTransactionAsync();

    Task SaveChangesAsync();

    // Refresh token management (delegated to token service infrastructure)
    // (no direct user repo method needed — TokenService handles via DbContext)
}
