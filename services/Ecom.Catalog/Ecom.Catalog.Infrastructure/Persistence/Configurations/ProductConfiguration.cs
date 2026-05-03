using Ecom.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Catalog.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.SKU).IsRequired().HasMaxLength(100);
        
        // 🔥 PERFORMANCE INDEXES
        builder.HasIndex(p => p.SKU).IsUnique();
        builder.HasIndex(p => new { p.Name, p.SKU }).HasDatabaseName("IX_Product_Search");
        builder.HasIndex(p => p.CategoryId).HasDatabaseName("IX_Products_CategoryId");
        builder.HasIndex(p => p.BrandId).HasDatabaseName("IX_Products_BrandId");
        builder.HasIndex(p => p.StatusId).HasDatabaseName("IX_Products_StatusId");
        builder.HasIndex(p => p.CreatedAt).HasDatabaseName("IX_Products_CreatedAt");
        builder.HasIndex(p => p.UpdatedAt).HasDatabaseName("IX_Products_UpdatedAt");
        
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId);
        builder.HasOne(p => p.Brand).WithMany().HasForeignKey(p => p.BrandId);
        builder.HasOne(p => p.Status).WithMany().HasForeignKey(p => p.StatusId);
    }
}
