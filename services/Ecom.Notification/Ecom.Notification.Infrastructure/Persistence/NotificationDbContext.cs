using Microsoft.EntityFrameworkCore;
using Ecom.Notification.Domain.Entities;

namespace Ecom.Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotificationMessage> Notifications => Set<NotificationMessage>();
    public DbSet<UserLoginHistory> UserLoginHistory => Set<UserLoginHistory>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}
