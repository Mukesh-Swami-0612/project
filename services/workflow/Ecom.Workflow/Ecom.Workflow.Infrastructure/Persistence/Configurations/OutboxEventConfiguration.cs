using Ecom.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Workflow.Infrastructure.Persistence.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.EventKey).IsUnique();
        builder.Property(o => o.Payload).HasColumnType("nvarchar(max)");
    }
}
