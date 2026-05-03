using Ecom.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Reporting.Infrastructure.Persistence.Configurations;

public class NotificationReadModelConfiguration : IEntityTypeConfiguration<NotificationReadModel>
{
    public void Configure(EntityTypeBuilder<NotificationReadModel> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NotificationId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Recipient).IsRequired().HasMaxLength(255);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.RetryCount).HasDefaultValue(0);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        // Composite index for trend queries
        builder.HasIndex(x => new { x.CreatedAt, x.Status })
            .HasDatabaseName("IX_Notifications_CreatedAt_Status");
        // Index for failure analysis
        builder.HasIndex(x => new { x.Status, x.FailureReason })
            .HasDatabaseName("IX_Notifications_Status_FailureReason");
    }
}
