using Ecom.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Workflow.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.ProductVariantId).IsUnique().HasDatabaseName("UQ_Inventory");
        builder.HasIndex(i => i.ProductVariantId).HasDatabaseName("IX_Inventory_VariantId");
        builder.Property(i => i.RowVersion).IsRowVersion();
        builder.ToTable(t => t.HasCheckConstraint("CHK_Stock", "Quantity >= 0"));
    }
}
