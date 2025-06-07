using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ConnectionScdDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScdDefaultStagingSchema",
                schema: "app",
                table: "SqlConnection",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScdDefaultStagingTableSuffix",
                schema: "app",
                table: "SqlConnection",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScdDefaultTargetSchema",
                schema: "app",
                table: "SqlConnection",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScdDefaultTargetTableSuffix",
                schema: "app",
                table: "SqlConnection",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScdDefaultStagingSchema",
                schema: "app",
                table: "SqlConnection");

            migrationBuilder.DropColumn(
                name: "ScdDefaultStagingTableSuffix",
                schema: "app",
                table: "SqlConnection");

            migrationBuilder.DropColumn(
                name: "ScdDefaultTargetSchema",
                schema: "app",
                table: "SqlConnection");

            migrationBuilder.DropColumn(
                name: "ScdDefaultTargetTableSuffix",
                schema: "app",
                table: "SqlConnection");
        }
    }
}
