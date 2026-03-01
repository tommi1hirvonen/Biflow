using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class VmStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AzureCredentialId",
                schema: "app",
                table: "Step",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VmOperation",
                schema: "app",
                table: "Step",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VmResourceId",
                schema: "app",
                table: "Step",
                type: "varchar(2048)",
                unicode: false,
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AzureCredentialId",
                schema: "app",
                table: "ExecutionStep",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VmOperation",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VmResourceId",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(2048)",
                unicode: false,
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Step_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "Step",
                column: "AzureCredentialId",
                principalSchema: "app",
                principalTable: "AzureCredential",
                principalColumn: "AzureCredentialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Step_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "AzureCredentialId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "VmOperation",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "VmResourceId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "AzureCredentialId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "VmOperation",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "VmResourceId",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
