using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecom.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TokenHashing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🔥 SECURITY: Invalidate all existing tokens
            // Old tokens are plain text, new system uses hashed tokens
            // Users will need to log in again
            migrationBuilder.Sql(@"
                UPDATE RefreshTokens 
                SET IsRevoked = 1, 
                    RevokedAt = GETUTCDATE()
                WHERE IsRevoked = 0;
            ");

            // Add comment to Token column to indicate it now stores hashes
            migrationBuilder.Sql(@"
                EXEC sp_addextendedproperty 
                    @name = N'MS_Description', 
                    @value = N'SHA256 hash of the refresh token (not plain text)', 
                    @level0type = N'SCHEMA', @level0name = N'dbo',
                    @level1type = N'TABLE',  @level1name = N'RefreshTokens',
                    @level2type = N'COLUMN', @level2name = N'Token';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot rollback token invalidation
            // Users would need to log in again anyway
        }
    }
}
