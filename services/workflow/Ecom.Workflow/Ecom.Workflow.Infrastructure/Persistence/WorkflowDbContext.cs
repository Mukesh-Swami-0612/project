using Ecom.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Workflow.Infrastructure.Persistence;

public class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options) { }

    public DbSet<Price> Prices => Set<Price>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryLog> InventoryLogs => Set<InventoryLog>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowDbContext).Assembly);
    }
}
