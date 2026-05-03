using System.ComponentModel.DataAnnotations;

namespace Ecom.Notification.Application.DTOs;

public class NotificationRequest
{
    [Required]
    [EmailAddress]
    public string ToEmail { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MinLength(5)]
    public string Body { get; set; } = string.Empty;
}

public class ScheduleNotificationRequest : NotificationRequest
{
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime? ScheduledAtIST { get; set; }
}
