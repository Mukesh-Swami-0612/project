namespace Ecom.Catalog.Domain.Entities;

/// <summary>
/// Outbox pattern implementation for reliable event publishing
/// Events are saved in the same transaction as domain changes
/// Background worker processes and publishes them to message broker
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public string Status { get; set; } = OutboxMessageStatus.Pending;
    public string? Error { get; set; }
    public int RetryCount { get; set; } = 0;
}

/// <summary>
/// Outbox message status constants
/// </summary>
public static class OutboxMessageStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
}
