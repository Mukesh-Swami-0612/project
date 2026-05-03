using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecom.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MigrationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MigrationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AppliedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MigrationLogs");
        }
    }
}
