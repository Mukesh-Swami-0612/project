using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ecom.Shared.Infrastructure.Services;

/// <summary>
/// 🔥 IDEMPOTENCY: Service for tracking processed messages to prevent duplicates
/// Uses in-memory concurrent dictionary for tracking
/// </summary>
public class IdempotencyService
{
    private readonly ILogger<IdempotencyService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _processedMessages = new();
    private readonly ConcurrentDictionary<string, DateTime> _processingLocks = new();
    private readonly TimeSpan _defaultExpiry = TimeSpan.FromHours(24);

    public IdempotencyService(ILogger<IdempotencyService> logger)
    {
        _logger = logger;
        
        // Start cleanup task
        _ = Task.Run(CleanupExpiredMessages);
    }

    /// <summary>
    /// Check if message was already processed
    /// </summary>
    public async Task<bool> IsProcessedAsync(string messageId)
    {
        if (string.IsNullOrEmpty(messageId))
            return false;

        // Check concurrent dictionary
        if (_processedMessages.TryGetValue(messageId, out var expiresAt))
        {
            if (expiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Message {MessageId} found in memory as processed", messageId);
                return true;
            }
            else
            {
                // Remove expired entry
                _processedMessages.TryRemove(messageId, out _);
            }
        }

        return false;
    }

    /// <summary>
    /// Mark message as processed with optional expiry
    /// </summary>
    public async Task MarkAsProcessedAsync(string messageId, TimeSpan? expiry = null)
    {
        if (string.IsNullOrEmpty(messageId))
            return;

        var expiryTime = expiry ?? _defaultExpiry;
        var expiresAt = DateTime.UtcNow.Add(expiryTime);

        // Store in concurrent dictionary
        _processedMessages.AddOrUpdate(messageId, expiresAt, (key, oldValue) => expiresAt);

        _logger.LogDebug("Message {MessageId} marked as processed, expires at {ExpiresAt}", 
            messageId, expiresAt);
    }

    /// <summary>
    /// Try to acquire processing lock for message (prevents concurrent processing)
    /// </summary>
    public async Task<bool> TryAcquireProcessingLockAsync(string messageId, TimeSpan? lockDuration = null)
    {
        if (string.IsNullOrEmpty(messageId))
            return false;

        var duration = lockDuration ?? TimeSpan.FromMinutes(5);
        var expiresAt = DateTime.UtcNow.Add(duration);

        // Try to add processing lock
        if (_processingLocks.TryGetValue(messageId, out var existingExpiry))
        {
            if (existingExpiry > DateTime.UtcNow)
            {
                _logger.LogDebug("Message {MessageId} is already being processed", messageId);
                return false;
            }
            else
            {
                // Lock expired, remove it
                _processingLocks.TryRemove(messageId, out _);
            }
        }

        _processingLocks.TryAdd(messageId, expiresAt);
        _logger.LogDebug("Acquired processing lock for message {MessageId}", messageId);
        return true;
    }

    /// <summary>
    /// Release processing lock for message
    /// </summary>
    public async Task ReleaseProcessingLockAsync(string messageId)
    {
        if (string.IsNullOrEmpty(messageId))
            return;

        _processingLocks.TryRemove(messageId, out _);
        _logger.LogDebug("Released processing lock for message {MessageId}", messageId);
    }

    /// <summary>
    /// Get processing statistics
    /// </summary>
    public async Task<IdempotencyStats> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var activeCount = _processedMessages.Count(kvp => kvp.Value > now);
        var expiredCount = _processedMessages.Count(kvp => kvp.Value <= now);

        return new IdempotencyStats
        {
            ActiveMessages = activeCount,
            ExpiredMessages = expiredCount,
            TotalMessages = _processedMessages.Count
        };
    }

    private async Task CleanupExpiredMessages()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(30)); // Cleanup every 30 minutes

                var now = DateTime.UtcNow;
                
                // Cleanup processed messages
                var expiredKeys = _processedMessages
                    .Where(kvp => kvp.Value <= now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _processedMessages.TryRemove(key, out _);
                }

                // Cleanup expired locks
                var expiredLocks = _processingLocks
                    .Where(kvp => kvp.Value <= now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredLocks)
                {
                    _processingLocks.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0 || expiredLocks.Count > 0)
                {
                    _logger.LogInformation("Cleaned up {MessageCount} expired idempotency records and {LockCount} expired locks", 
                        expiredKeys.Count, expiredLocks.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during idempotency cleanup");
            }
        }
    }
}

public class IdempotencyStats
{
    public int ActiveMessages { get; set; }
    public int ExpiredMessages { get; set; }
    public int TotalMessages { get; set; }
}