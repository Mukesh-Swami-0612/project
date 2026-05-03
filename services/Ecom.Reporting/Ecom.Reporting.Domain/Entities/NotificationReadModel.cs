namespace Ecom.Reporting.Domain.Entities;

public class NotificationReadModel
{
    public int Id { get; set; }
    public string NotificationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Sent, Failed, Pending
    public string Type { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}
