using Ecom.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecom.Auth.API.BackgroundServices;

/// <summary>
/// A background service that periodically cleans up expired and revoked refresh tokens from the database.
/// This prevents the RefreshTokens table from growing indefinitely.
/// </summary>
public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6); // Run every 6 hours

    public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Token Cleanup Service is running a cleanup task.");

            try
            {
                await CleanupTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning up tokens.");
            }

            // Wait for the next interval or until cancellation is requested
            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Token Cleanup Service is stopping.");
    }

    private async Task CleanupTokensAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        // Remove tokens that are either expired OR revoked
        // industry standard: keep tokens for a short grace period after expiry if needed, but here we just purge.
        var expiredTokens = await context.RefreshTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow || t.IsRevoked)
            .ToListAsync(stoppingToken);

        if (expiredTokens.Any())
        {
            _logger.LogInformation("Found {Count} expired/revoked tokens. Removing them...", expiredTokens.Count);
            context.RefreshTokens.RemoveRange(expiredTokens);
            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Successfully removed tokens.");
        }
        else
        {
            _logger.LogInformation("No expired or revoked tokens found.");
        }
    }
}
