using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecom.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "WorkflowInstances",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "WorkflowInstances",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "WorkflowInstances");
        }
    }
}
