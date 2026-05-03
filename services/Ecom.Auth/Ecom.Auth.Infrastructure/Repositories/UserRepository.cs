using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.Domain.Entities;
using Ecom.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ecom.Auth.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context) => _context = context;

    public Task<User?> GetByEmailAsync(string email) =>
        _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

    public Task<User?> GetByIdAsync(int id) =>
        _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _context.Users.Include(u => u.Role).Where(u => !u.IsDeleted).ToListAsync();

    public async Task AddAsync(User user, bool saveChanges = true)
    {
        await _context.Users.AddAsync(user);
        if (saveChanges)
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(User user, bool saveChanges = true)
    {
        _context.Users.Update(user);
        if (saveChanges)
        {
            await _context.SaveChangesAsync();
        }
    }

    // ── Email Verification Tokens ────────────────────────────────────────────────
    public async Task AddEmailVerificationTokenAsync(EmailVerificationToken token, bool saveChanges = true)
    {
        await _context.EmailVerificationTokens.AddAsync(token);
        if (saveChanges)
        {
            await _context.SaveChangesAsync();
        }
    }

    public Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(string token) =>
        _context.EmailVerificationTokens
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Token == token);

    // ── Role Validation ───────────────────────────────────────────────────────────
    public Task<bool> RoleExistsAsync(int roleId) =>
        _context.Roles.AnyAsync(r => r.Id == roleId);

    // ── Transaction Support ───────────────────────────────────────────────────────
    public Task<IDbContextTransaction> BeginTransactionAsync() =>
        _context.Database.BeginTransactionAsync();

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
