using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EndpointConcurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentPipelineSteps",
                schema: "app",
                table: "PipelineClient",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentFunctionSteps",
                schema: "app",
                table: "FunctionApp",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentPackageSteps",
                schema: "app",
                table: "Connection",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentSqlSteps",
                schema: "app",
                table: "Connection",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxConcurrentPipelineSteps",
                schema: "app",
                table: "PipelineClient");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentFunctionSteps",
                schema: "app",
                table: "FunctionApp");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentPackageSteps",
                schema: "app",
                table: "Connection");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentSqlSteps",
                schema: "app",
                table: "Connection");
        }
    }
}
