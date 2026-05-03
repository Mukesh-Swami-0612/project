using Ecom.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Reporting.Infrastructure.Persistence.Configurations;

public class ProductReadModelConfiguration : IEntityTypeConfiguration<ProductReadModel>
{
    public void Configure(EntityTypeBuilder<ProductReadModel> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(255);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IsLowStock);
        builder.HasIndex(x => x.CreatedAt);
        // Composite index for trend queries
        builder.HasIndex(x => new { x.CreatedAt, x.Status })
            .HasDatabaseName("IX_Products_CreatedAt_Status");
        // Index for rejection analysis
        builder.HasIndex(x => new { x.Status, x.RejectionReason })
            .HasDatabaseName("IX_Products_Status_RejectionReason");
        // Index for hotspot detection
        builder.HasIndex(x => new { x.Status, x.ProductId })
            .HasDatabaseName("IX_Products_Status_ProductId");
    }
}
