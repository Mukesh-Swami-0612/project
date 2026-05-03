namespace Ecom.Shared.Infrastructure.Configuration;

/// <summary>
/// 🔥 RESILIENCE: Configuration options for RabbitMQ reliability features
/// </summary>
public class RabbitMqReliabilityOptions
{
    public const string SectionName = "RabbitMq:Reliability";

    /// <summary>
    /// Maximum number of retry attempts before sending to DLQ
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff retry (in seconds)
    /// </summary>
    public int BaseRetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Message TTL in minutes
    /// </summary>
    public int MessageTtlMinutes { get; set; } = 30;

    /// <summary>
    /// Circuit breaker failure threshold
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker break duration in seconds
    /// </summary>
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Enable idempotency checking
    /// </summary>
    public bool EnableIdempotency { get; set; } = true;

    /// <summary>
    /// Idempotency cache expiry in hours
    /// </summary>
    public int IdempotencyCacheExpiryHours { get; set; } = 24;

    /// <summary>
    /// Enable automatic DLQ creation
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Enable connection recovery
    /// </summary>
    public bool EnableConnectionRecovery { get; set; } = true;

    /// <summary>
    /// Connection recovery interval in seconds
    /// </summary>
    public int ConnectionRecoveryIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Heartbeat interval in seconds
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 60;
}