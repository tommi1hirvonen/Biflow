using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FabricStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FabricItemId",
                schema: "app",
                table: "Step",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FabricItemName",
                schema: "app",
                table: "Step",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FabricItemType",
                schema: "app",
                table: "Step",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FabricWorkspaceId",
                schema: "app",
                table: "Step",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FabricWorkspaceName",
                schema: "app",
                table: "Step",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobInstanceId",
                schema: "app",
                table: "ExecutionStepAttempt",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FabricItemId",
                schema: "app",
                table: "ExecutionStep",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FabricItemName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FabricItemType",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FabricWorkspaceId",
                schema: "app",
                table: "ExecutionStep",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FabricWorkspaceName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FabricItemId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "FabricItemName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "FabricItemType",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "FabricWorkspaceId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "FabricWorkspaceName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "JobInstanceId",
                schema: "app",
                table: "ExecutionStepAttempt");

            migrationBuilder.DropColumn(
                name: "FabricItemId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "FabricItemName",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "FabricItemType",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "FabricWorkspaceId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "FabricWorkspaceName",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
