using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Catalog.Infrastructure.Persistence.Configurations;

public class ProductStatusConfiguration : IEntityTypeConfiguration<ProductStatus>
{
    public void Configure(EntityTypeBuilder<ProductStatus> builder)
    {
        builder.ToTable("ProductStatuses");

        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Id)
            .ValueGeneratedOnAdd(); // Keep IDENTITY for now

        builder.Property(ps => ps.Name)
            .HasColumnName("StatusName") // Map to existing column
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ps => ps.Description)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue(""); // Allow empty for now

        // 🔥 SEED DATA: Populate lookup table with all lifecycle statuses
        builder.HasData(
            new ProductStatus
            {
                Id = (int)ProductLifecycleStatus.Draft,
                Name = "Draft",
                Description = "Product is in draft state and being created"
            },
            new ProductStatus
            {
                Id = (int)ProductLifecycleStatus.InEnrichment,
                Name = "InEnrichment",
                Description = "Product is being enriched with content and details"
            },
            new ProductStatus
            {
                Id = (int)ProductLifecycleStatus.ReadyForReview,
                Name = "ReadyForReview",
                Description = "Product has been submitted for approval"
            },
            new ProductStatus
            {
                Id = (int)ProductLifecycleStatus.Approved,
                Name = "Approved",
                Description = "Product has been approved and ready to publish"
            },
            new ProductStatus
            {
                Id = (int)ProductLifecycleStatus.Published,
                Name = "Published",
                Description = "Product is live and visible to customers"
            },
            new ProductStatus
            {
                Id = (int)ProductLifecycleStatus.Rejected,
                Name = "Rejected",
                Description = "Product was rejected and needs rework"
            },
            new ProductStatus
            {
                Id = (int)ProductLifecycleStatus.Archived,
                Name = "Archived",
                Description = "Product is archived and no longer active"
            }
        );
    }
}
