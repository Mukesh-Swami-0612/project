using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecom.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SessionManagementAndRaceConditionProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceInfo",
                table: "RefreshTokens",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "RefreshTokens",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RefreshTokens",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "RefreshTokens",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceInfo",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RefreshTokens");
        }
    }
}
