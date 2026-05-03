using Ecom.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Workflow.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventId)
            .IsRequired();

        builder.HasIndex(x => x.EventId)
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyRecords_EventId");

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ProcessedAt)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);
    }
}
