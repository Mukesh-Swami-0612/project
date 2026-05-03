using Ecom.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Catalog.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.UserEmail).IsRequired().HasMaxLength(256);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.Timestamp);
    }
}
