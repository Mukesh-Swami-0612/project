using Ecom.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Reporting.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityName).HasMaxLength(100);
        builder.Property(a => a.Action).HasMaxLength(50);
        builder.Property(a => a.EventType).HasMaxLength(100);
        builder.Property(a => a.SourceService).HasMaxLength(50);
        builder.Property(a => a.CorrelationId).HasMaxLength(100);
        builder.HasIndex(a => new { a.EntityName, a.EntityId }).HasDatabaseName("IX_Audit_Entity");
    }
}
