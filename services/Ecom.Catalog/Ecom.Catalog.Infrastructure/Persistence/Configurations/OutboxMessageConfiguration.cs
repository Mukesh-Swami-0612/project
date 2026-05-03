using Ecom.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Catalog.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(o => o.Payload)
            .IsRequired();

        builder.Property(o => o.OccurredOn)
            .IsRequired();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(OutboxMessageStatus.Pending);

        builder.Property(o => o.Error)
            .HasMaxLength(2000);

        builder.Property(o => o.RetryCount)
            .HasDefaultValue(0);

        // 🔥 PERFORMANCE: Index for background worker queries
        builder.HasIndex(o => new { o.Status, o.OccurredOn })
            .HasDatabaseName("IX_OutboxMessages_Status_OccurredOn");

        // 🔥 PERFORMANCE: Index for cleanup queries
        builder.HasIndex(o => o.ProcessedOn)
            .HasDatabaseName("IX_OutboxMessages_ProcessedOn");
    }
}
