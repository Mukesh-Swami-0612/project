namespace Ecom.Notification.Domain.Entities;

/// <summary>
/// Notification entity for event-driven communication
/// Supports both immediate and scheduled delivery
/// </summary>
public class NotificationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Notification type (e.g., PRODUCT_APPROVED, USER_REGISTERED)
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Recipient email address
    /// </summary>
    public string ToEmail { get; set; } = string.Empty;
    
    public string Subject { get; set; } = string.Empty;
    
    public string Body { get; set; } = string.Empty;
    
    /// <summary>
    /// Correlation ID for tracing across services
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Status: Pending, Sent, Failed
    /// </summary>
    public string Status { get; set; } = "Pending";
    
    public bool IsSent { get; set; } = false;

    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Next retry timestamp for exponential backoff (null = retry immediately)
    /// </summary>
    public DateTime? NextRetryAt { get; set; }
    
    public DateTime? ScheduledAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? SentAt { get; set; }
}
