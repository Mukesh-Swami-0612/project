using Ecom.Notification.Domain.Entities;
using Ecom.Notification.Domain.Interfaces;
using Ecom.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Notification.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NotificationMessage message)
    {
        await _context.Notifications.AddAsync(message);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Check if notification already exists (for idempotency)
    /// </summary>
    public async Task<bool> ExistsAsync(string correlationId, string type)
    {
        return await _context.Notifications
            .AnyAsync(x => x.CorrelationId == correlationId && x.Type == type);
    }
}
