namespace Ecom.Notification.Application.Interfaces;

/// <summary>
/// Service for tracking processed events (Phase 1: Tracking only, no blocking)
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Check if an event has already been processed
    /// </summary>
    Task<bool> IsProcessedAsync(Guid eventId);

    /// <summary>
    /// Mark an event as processed
    /// </summary>
    Task MarkProcessedAsync(Guid eventId, string eventType);

    /// <summary>
    /// Execute business logic within a transaction that includes idempotency marking
    /// Phase 2: Ensures atomicity - either both succeed or both fail
    /// </summary>
    Task ExecuteWithIdempotencyAsync(Guid eventId, string eventType, Func<Task> businessLogic);
}
