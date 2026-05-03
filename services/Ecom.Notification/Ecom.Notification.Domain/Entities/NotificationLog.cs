namespace Ecom.Notification.Domain.Entities;

/// <summary>
/// Entity representing a log entry in the NotificationLogs table
/// Used for querying logs from the database
/// </summary>
public class NotificationLog
{
    public int Id { get; set; }
    
    public string Level { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public string? CorrelationId { get; set; }
    
    public string? Username { get; set; }
    
    public Guid? NotificationId { get; set; }
    
    public string? MachineName { get; set; }
    
    public int? ThreadId { get; set; }
    
    public string? Exception { get; set; }
    
    public string? Properties { get; set; }
    
    public DateTime LoggedAt { get; set; }
}
