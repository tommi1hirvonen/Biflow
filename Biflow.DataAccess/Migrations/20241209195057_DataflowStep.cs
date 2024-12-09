using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DataflowStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DatasetId",
                schema: "app",
                table: "Step",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DatasetGroupId",
                schema: "app",
                table: "Step",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowGroupId",
                schema: "app",
                table: "Step",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowId",
                schema: "app",
                table: "Step",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DatasetId",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DatasetGroupId",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowGroupId",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowId",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataflowGroupId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DataflowId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DataflowGroupId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DataflowId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.AlterColumn<string>(
                name: "DatasetId",
                schema: "app",
                table: "Step",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DatasetGroupId",
                schema: "app",
                table: "Step",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DatasetId",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DatasetGroupId",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36,
                oldNullable: true);
        }
    }
}
