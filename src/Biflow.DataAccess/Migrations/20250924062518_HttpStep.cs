using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class HttpStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HttpBody",
                schema: "app",
                table: "Step",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpBodyFormat",
                schema: "app",
                table: "Step",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HttpDisableAsyncPattern",
                schema: "app",
                table: "Step",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpHeaders",
                schema: "app",
                table: "Step",
                type: "varchar(max)",
                unicode: false,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                schema: "app",
                table: "Step",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpUrl",
                schema: "app",
                table: "Step",
                type: "varchar(2048)",
                unicode: false,
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpBody",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HttpDisableAsyncPattern",
                schema: "app",
                table: "ExecutionStep",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpHeaders",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(max)",
                unicode: false,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpUrl",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(2048)",
                unicode: false,
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HttpBody",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "HttpBodyFormat",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "HttpDisableAsyncPattern",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "HttpHeaders",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "HttpUrl",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "HttpBody",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "HttpDisableAsyncPattern",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "HttpHeaders",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "HttpUrl",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
