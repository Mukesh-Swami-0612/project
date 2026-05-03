using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecom.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if indexes already exist before creating them
            // Note: Using ExpiryDate as that's the actual column name in the database
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId_IsRevoked_Expiry' AND object_id = OBJECT_ID('RefreshTokens'))
                BEGIN
                    CREATE INDEX [IX_RefreshTokens_UserId_IsRevoked_Expiry] ON [RefreshTokens] ([UserId], [IsRevoked], [ExpiryDate]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId' AND object_id = OBJECT_ID('RefreshTokens'))
                BEGIN
                    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked_Expiry",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");
        }
    }
}
