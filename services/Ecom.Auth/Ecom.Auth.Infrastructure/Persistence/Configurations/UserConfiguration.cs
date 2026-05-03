using Ecom.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Auth.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
        builder.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
    }
}
