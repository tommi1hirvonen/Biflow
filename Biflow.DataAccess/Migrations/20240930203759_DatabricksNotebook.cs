using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DatabricksNotebook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DatabricksWorkspaceId",
                schema: "app",
                table: "Step",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotebookPath",
                schema: "app",
                table: "Step",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "JobRunId",
                schema: "app",
                table: "ExecutionStepAttempt",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DatabricksWorkspaceId",
                schema: "app",
                table: "ExecutionStep",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotebookPath",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DatabricksWorkspace",
                schema: "app",
                columns: table => new
                {
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    WorkspaceUrl = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ApiToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabricksWorkspace", x => x.WorkspaceId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Step_DatabricksWorkspace_DatabricksWorkspaceId",
                schema: "app",
                table: "Step",
                column: "DatabricksWorkspaceId",
                principalSchema: "app",
                principalTable: "DatabricksWorkspace",
                principalColumn: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Step_DatabricksWorkspace_DatabricksWorkspaceId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropTable(
                name: "DatabricksWorkspace",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "DatabricksWorkspaceId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "NotebookPath",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "JobRunId",
                schema: "app",
                table: "ExecutionStepAttempt");

            migrationBuilder.DropColumn(
                name: "DatabricksWorkspaceId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "NotebookPath",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
