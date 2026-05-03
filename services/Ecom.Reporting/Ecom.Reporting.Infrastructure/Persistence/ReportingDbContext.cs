using Ecom.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Reporting.Infrastructure.Persistence;

public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<NotificationReadModel> Notifications => Set<NotificationReadModel>();
    public DbSet<WorkflowReadModel> Workflows => Set<WorkflowReadModel>();
    public DbSet<ProductReadModel> Products => Set<ProductReadModel>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
    }
}
