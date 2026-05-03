using Ecom.Notification.Application.DTOs;
using Ecom.Notification.Domain.Entities;

namespace Ecom.Notification.Application.Interfaces;

public interface INotificationService
{
    Task<bool> SendNowAsync(NotificationRequest request);
    Task<bool> ScheduleAsync(ScheduleNotificationRequest request);
    Task<bool> ProcessScheduledAsync(NotificationMessage message);
    
    /// <summary>
    /// Create notification from event (event-driven)
    /// </summary>
    Task SendEventNotificationAsync(string type, string subject, string body, string toEmail, string? correlationId);
}
