using Ecom.Notification.Domain.Entities;

namespace Ecom.Notification.Domain.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(NotificationMessage message);
    Task SaveChangesAsync();
    
    /// <summary>
    /// Check if notification already exists (for idempotency)
    /// </summary>
    Task<bool> ExistsAsync(string correlationId, string type);
}
