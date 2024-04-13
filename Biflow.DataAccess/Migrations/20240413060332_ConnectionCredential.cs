using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ConnectionCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CredentialId",
                schema: "app",
                table: "Connection",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Connection_Credential_CredentialId",
                schema: "app",
                table: "Connection",
                column: "CredentialId",
                principalSchema: "app",
                principalTable: "Credential",
                principalColumn: "CredentialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Connection_Credential_CredentialId",
                schema: "app",
                table: "Connection");

            migrationBuilder.DropColumn(
                name: "CredentialId",
                schema: "app",
                table: "Connection");
        }
    }
}
