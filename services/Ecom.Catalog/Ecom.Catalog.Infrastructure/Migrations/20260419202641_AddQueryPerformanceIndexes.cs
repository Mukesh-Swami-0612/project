using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecom.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedAt",
                table: "Products",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UpdatedAt",
                table: "Products",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_CreatedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_UpdatedAt",
                table: "Products");
        }
    }
}
