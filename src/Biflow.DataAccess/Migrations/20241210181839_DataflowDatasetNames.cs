using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DataflowDatasetNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataflowGroupName",
                schema: "app",
                table: "Step",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowName",
                schema: "app",
                table: "Step",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatasetGroupName",
                schema: "app",
                table: "Step",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatasetName",
                schema: "app",
                table: "Step",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowGroupName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatasetGroupName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatasetName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataflowGroupName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DataflowName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DatasetGroupName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DatasetName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DataflowGroupName",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DataflowName",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DatasetGroupName",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DatasetName",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
