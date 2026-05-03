using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecom.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if columns exist before adding them
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RefreshTokens]') AND name = 'ReplacedByToken')
                BEGIN
                    ALTER TABLE [RefreshTokens] ADD [ReplacedByToken] nvarchar(512) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RefreshTokens]') AND name = 'RevokedAt')
                BEGIN
                    ALTER TABLE [RefreshTokens] ADD [RevokedAt] datetime2 NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReplacedByToken",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "RefreshTokens");
        }
    }
}
