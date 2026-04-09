using Ecom.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Catalog.Infrastructure.Persistence.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.FileUrl).IsRequired().HasMaxLength(500);
        builder.Property(m => m.FileType).HasMaxLength(50);
        builder.Property(m => m.AltText).HasMaxLength(255);
        builder.Property(m => m.RowVersion).IsRowVersion();
        builder.HasOne(m => m.Product).WithMany(p => p.MediaAssets)
            .HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
