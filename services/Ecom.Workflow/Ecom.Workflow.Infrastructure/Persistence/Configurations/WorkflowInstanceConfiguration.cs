using Ecom.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Workflow.Infrastructure.Persistence.Configurations;

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CurrentStep);
        builder.HasIndex(x => x.CorrelationId);
        
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.CurrentStep)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.ProductId)
            .IsRequired();
        
        builder.Property(x => x.RetryCount)
            .HasDefaultValue(0);
        
        builder.Property(x => x.MaxRetries)
            .HasDefaultValue(3);
        
        builder.Property(x => x.NextRetryAt)
            .IsRequired(false);
        
        builder.Property(x => x.LastError)
            .HasMaxLength(2000);
        
        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}
