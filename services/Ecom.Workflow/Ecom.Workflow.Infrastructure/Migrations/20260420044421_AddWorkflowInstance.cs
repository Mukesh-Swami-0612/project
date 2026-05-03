using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecom.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Approvals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepNumber = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.CheckConstraint("CHK_Stock", "Quantity >= 0");
                });

            migrationBuilder.CreateTable(
                name: "InventoryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    ChangeQty = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    MRP = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prices", x => x.Id);
                    table.CheckConstraint("CHK_Price", "SalePrice <= MRP");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_VariantId",
                table: "Inventories",
                column: "ProductVariantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_EventKey",
                table: "OutboxEvents",
                column: "EventKey",
                unique: true,
                filter: "[EventKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Prices_EventKey",
                table: "Prices",
                column: "EventKey",
                unique: true,
                filter: "[EventKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Prices_VariantId",
                table: "Prices",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_CorrelationId",
                table: "WorkflowInstances",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_CurrentStep",
                table: "WorkflowInstances",
                column: "CurrentStep");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_ProductId",
                table: "WorkflowInstances",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_Status",
                table: "WorkflowInstances",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Approvals");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "InventoryLogs");

            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropTable(
                name: "Prices");

            migrationBuilder.DropTable(
                name: "WorkflowInstances");
        }
    }
}
