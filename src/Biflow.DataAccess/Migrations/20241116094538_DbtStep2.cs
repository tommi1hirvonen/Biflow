using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DbtStep2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DbtJobEnvironmentId",
                schema: "app",
                table: "Step",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DbtJobEnvironmentName",
                schema: "app",
                table: "Step",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DbtJobProjectId",
                schema: "app",
                table: "Step",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DbtJobProjectName",
                schema: "app",
                table: "Step",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DbtJobEnvironmentId",
                schema: "app",
                table: "ExecutionStep",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DbtJobEnvironmentName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DbtJobProjectId",
                schema: "app",
                table: "ExecutionStep",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DbtJobProjectName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DbtJobEnvironmentId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DbtJobEnvironmentName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DbtJobProjectId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DbtJobProjectName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DbtJobEnvironmentId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DbtJobEnvironmentName",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DbtJobProjectId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DbtJobProjectName",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
