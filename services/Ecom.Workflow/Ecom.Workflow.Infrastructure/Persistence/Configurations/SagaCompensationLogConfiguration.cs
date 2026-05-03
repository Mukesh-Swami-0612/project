using Ecom.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Workflow.Infrastructure.Persistence.Configurations;

public class SagaCompensationLogConfiguration : IEntityTypeConfiguration<SagaCompensationLog>
{
    public void Configure(EntityTypeBuilder<SagaCompensationLog> builder)
    {
        builder.ToTable("SagaCompensationLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkflowId)
            .IsRequired();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.FailedAtStep)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.CompensationAction)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.CompensationDetails)
            .HasMaxLength(1000);

        builder.Property(x => x.CompensationSuccessful)
            .IsRequired();

        builder.Property(x => x.CompensationError)
            .HasMaxLength(1000);

        builder.Property(x => x.ExecutedAt)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        // Indexes for querying
        builder.HasIndex(x => x.WorkflowId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.ExecutedAt);
    }
}
