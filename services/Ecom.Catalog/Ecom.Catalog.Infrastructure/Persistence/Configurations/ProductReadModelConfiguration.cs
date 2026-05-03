using Ecom.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Catalog.Infrastructure.Persistence.Configurations;

public class ProductReadModelConfiguration : IEntityTypeConfiguration<ProductReadModel>
{
    public void Configure(EntityTypeBuilder<ProductReadModel> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ProductName).IsRequired().HasMaxLength(150);
        builder.Property(p => p.CategoryName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(50);
    }
}
