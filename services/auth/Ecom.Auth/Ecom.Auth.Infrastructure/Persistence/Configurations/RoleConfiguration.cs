using Ecom.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Auth.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RoleName).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.RoleName).IsUnique();

        builder.HasData(
            new Role { Id = 1, RoleName = "Admin" },
            new Role { Id = 2, RoleName = "ProductManager" },
            new Role { Id = 3, RoleName = "ContentExecutive" },
            new Role { Id = 4, RoleName = "Customer" }
        );
    }
}
