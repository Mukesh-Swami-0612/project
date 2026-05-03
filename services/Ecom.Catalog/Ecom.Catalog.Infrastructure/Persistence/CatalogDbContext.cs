using Ecom.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductStatus> ProductStatuses => Set<ProductStatus>();
    public DbSet<ProductVariantCombination> ProductVariants => Set<ProductVariantCombination>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<ProductReadModel> ProductReadModels => Set<ProductReadModel>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<MigrationLog> MigrationLogs => Set<MigrationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
