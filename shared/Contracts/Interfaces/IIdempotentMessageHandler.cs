namespace Ecom.Shared.Contracts.Interfaces;

/// <summary>
/// 🔥 IDEMPOTENCY: Interface for handlers that support idempotent message processing
/// </summary>
public interface IIdempotentMessageHandler<T> where T : class
{
    /// <summary>
    /// Process message with idempotency key
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="idempotencyKey">Unique key to ensure idempotent processing</param>
    /// <returns>True if processed successfully, false if should retry</returns>
    Task<bool> HandleAsync(T message, string idempotencyKey);
    
    /// <summary>
    /// Check if message was already processed
    /// </summary>
    /// <param name="idempotencyKey">Unique key to check</param>
    /// <returns>True if already processed</returns>
    Task<bool> IsProcessedAsync(string idempotencyKey);
}