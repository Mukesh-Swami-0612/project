using Ecom.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Workflow.Infrastructure.Persistence.Configurations;

public class PriceConfiguration : IEntityTypeConfiguration<Price>
{
    public void Configure(EntityTypeBuilder<Price> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.MRP).HasColumnType("decimal(10,2)");
        builder.Property(p => p.SalePrice).HasColumnType("decimal(10,2)");
        builder.HasIndex(p => p.ProductVariantId).HasDatabaseName("IX_Prices_VariantId");
        builder.HasIndex(p => p.EventKey).IsUnique();
        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.ToTable(t => t.HasCheckConstraint("CHK_Price", "SalePrice <= MRP"));
    }
}
