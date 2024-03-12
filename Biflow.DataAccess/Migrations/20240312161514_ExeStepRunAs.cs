using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ExeStepRunAs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExeDomain",
                schema: "app",
                table: "Step",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExePassword",
                schema: "app",
                table: "Step",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExeUsername",
                schema: "app",
                table: "Step",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExeDomain",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExePassword",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExeUsername",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExeDomain",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "ExePassword",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "ExeUsername",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "ExeDomain",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "ExePassword",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "ExeUsername",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
