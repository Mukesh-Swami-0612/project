using Ecom.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Reporting.Infrastructure.Persistence.Configurations;

public class WorkflowReadModelConfiguration : IEntityTypeConfiguration<WorkflowReadModel>
{
    public void Configure(EntityTypeBuilder<WorkflowReadModel> builder)
    {
        builder.ToTable("Workflows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WorkflowId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.Property(x => x.WorkflowType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.RetryCount).HasDefaultValue(0);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        // Composite index for trend queries
        builder.HasIndex(x => new { x.CreatedAt, x.Status })
            .HasDatabaseName("IX_Workflows_CreatedAt_Status");
        // Index for failure analysis
        builder.HasIndex(x => new { x.Status, x.FailureReason })
            .HasDatabaseName("IX_Workflows_Status_FailureReason");
        // Index for hotspot detection
        builder.HasIndex(x => new { x.Status, x.WorkflowId })
            .HasDatabaseName("IX_Workflows_Status_WorkflowId");
    }
}
