using Ecom.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecom.Auth.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(512)
            .HasColumnName("Token"); // Map to existing Token column

        builder.Property(t => t.ExpiresAt)
            .HasColumnName("ExpiryDate");

        builder.Property(t => t.RevokedAt)
            .IsRequired(false);

        builder.Property(t => t.ReplacedByTokenHash)
            .HasMaxLength(512)
            .IsRequired(false)
            .HasColumnName("ReplacedByToken"); // Map to existing column

        // 🔥 SESSION MANAGEMENT: Device tracking
        builder.Property(t => t.DeviceInfo)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(t => t.IpAddress)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired(false);

        builder.Property(t => t.UserAgent)
            .HasMaxLength(512)
            .IsRequired(false);

        // 🔥 RACE CONDITION PROTECTION: Optimistic concurrency control
        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .IsRequired();

        // 🔥 PERFORMANCE: Unique index on TokenHash for fast lookups
        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        // 🔥 PERFORMANCE: Index on UserId for fast user token queries
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        // 🔥 PERFORMANCE: Composite index for active token queries
        builder.HasIndex(t => new { t.UserId, t.IsRevoked, t.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_UserId_IsRevoked_Expiry");

        builder.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
