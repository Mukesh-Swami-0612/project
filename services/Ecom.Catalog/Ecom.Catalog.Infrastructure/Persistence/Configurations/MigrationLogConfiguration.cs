using Ecom.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Catalog.Infrastructure.Persistence.Configurations;

public class MigrationLogConfiguration : IEntityTypeConfiguration<MigrationLog>
{
    public void Configure(EntityTypeBuilder<MigrationLog> builder)
    {
        builder.ToTable("MigrationLogs");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.MigrationName).IsRequired().HasMaxLength(200);
        builder.Property(m => m.AppliedBy).IsRequired().HasMaxLength(100);
        builder.Property(m => m.AppliedAt).IsRequired();
        builder.Property(m => m.Details).HasMaxLength(1000);
    }
}
