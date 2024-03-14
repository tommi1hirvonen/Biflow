using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RunAsCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExeRunAsCredentialId",
                schema: "app",
                table: "Step",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExeRunAsCredentialId",
                schema: "app",
                table: "ExecutionStep",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Credential",
                schema: "app",
                columns: table => new
                {
                    CredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credential", x => x.CredentialId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Step_Credential_ExeRunAsCredentialId",
                schema: "app",
                table: "Step",
                column: "ExeRunAsCredentialId",
                principalSchema: "app",
                principalTable: "Credential",
                principalColumn: "CredentialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Step_Credential_ExeRunAsCredentialId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropTable(
                name: "Credential",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "ExeRunAsCredentialId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "ExeRunAsCredentialId",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
