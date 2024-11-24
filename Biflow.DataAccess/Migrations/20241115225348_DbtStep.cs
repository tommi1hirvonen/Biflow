using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DbtStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DbtAccountId",
                schema: "app",
                table: "Step",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DbtJobId",
                schema: "app",
                table: "Step",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DbtJobName",
                schema: "app",
                table: "Step",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DbtJobRunId",
                schema: "app",
                table: "ExecutionStepAttempt",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DbtAccountId",
                schema: "app",
                table: "ExecutionStep",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DbtJobId",
                schema: "app",
                table: "ExecutionStep",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DbtJobName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DbtAccount",
                schema: "app",
                columns: table => new
                {
                    DbtAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DbtAccountName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApiToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbtAccount", x => x.DbtAccountId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Step_DbtAccount_DbtAccountId",
                schema: "app",
                table: "Step",
                column: "DbtAccountId",
                principalSchema: "app",
                principalTable: "DbtAccount",
                principalColumn: "DbtAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Step_DbtAccount_DbtAccountId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropTable(
                name: "DbtAccount",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "DbtAccountId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DbtJobId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DbtJobName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DbtJobRunId",
                schema: "app",
                table: "ExecutionStepAttempt");

            migrationBuilder.DropColumn(
                name: "DbtAccountId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DbtJobId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DbtJobName",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
