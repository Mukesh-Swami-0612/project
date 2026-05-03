using Ecom.Catalog.Domain.Entities;

namespace Ecom.Catalog.Application.Interfaces;

/// <summary>
/// Repository for managing outbox messages
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Add a new outbox message (called within domain transaction)
    /// </summary>
    Task AddAsync(OutboxMessage message);

    /// <summary>
    /// Get pending messages for processing (background worker)
    /// </summary>
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 20);

    /// <summary>
    /// Mark message as processing (to prevent duplicate processing)
    /// </summary>
    Task MarkAsProcessingAsync(Guid messageId);

    /// <summary>
    /// Mark message as processed successfully
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId);

    /// <summary>
    /// Mark message as failed with error details
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, int retryCount);

    /// <summary>
    /// Get failed messages that can be retried
    /// </summary>
    Task<IEnumerable<OutboxMessage>> GetRetryableMessagesAsync(int maxRetries = 3);

    /// <summary>
    /// Delete old processed messages (cleanup)
    /// </summary>
    Task DeleteOldProcessedMessagesAsync(DateTime olderThan);
}
