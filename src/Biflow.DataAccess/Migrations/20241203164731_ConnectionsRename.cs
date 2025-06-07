using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ConnectionsRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Connection_Credential_CredentialId",
                schema: "app",
                table: "Connection");

            migrationBuilder.DropForeignKey(
                name: "FK_DataTable_Connection_ConnectionId",
                schema: "app",
                table: "DataTable");

            migrationBuilder.DropForeignKey(
                name: "FK_ScdTable_Connection_ConnectionId",
                schema: "app",
                table: "ScdTable");

            migrationBuilder.DropForeignKey(
                name: "FK_Step_Connection_ConnectionId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Connection",
                schema: "app",
                table: "Connection");

            migrationBuilder.RenameTable(
                name: "Connection",
                schema: "app",
                newName: "SqlConnection",
                newSchema: "app");

            migrationBuilder.RenameColumn(
                name: "ConnectionType",
                schema: "app",
                table: "SqlConnection",
                newName: "SqlConnectionType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SqlConnection",
                schema: "app",
                table: "SqlConnection",
                column: "ConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DataTable_SqlConnection_ConnectionId",
                schema: "app",
                table: "DataTable",
                column: "ConnectionId",
                principalSchema: "app",
                principalTable: "SqlConnection",
                principalColumn: "ConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScdTable_SqlConnection_ConnectionId",
                schema: "app",
                table: "ScdTable",
                column: "ConnectionId",
                principalSchema: "app",
                principalTable: "SqlConnection",
                principalColumn: "ConnectionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SqlConnection_Credential_CredentialId",
                schema: "app",
                table: "SqlConnection",
                column: "CredentialId",
                principalSchema: "app",
                principalTable: "Credential",
                principalColumn: "CredentialId");

            migrationBuilder.AddForeignKey(
                name: "FK_Step_SqlConnection_ConnectionId",
                schema: "app",
                table: "Step",
                column: "ConnectionId",
                principalSchema: "app",
                principalTable: "SqlConnection",
                principalColumn: "ConnectionId");

            migrationBuilder.Sql("""
                update a
                set [SqlConnectionType] = 'MsSql'
                from [app].[SqlConnection] as a
                where [SqlConnectionType] = 'Sql'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataTable_SqlConnection_ConnectionId",
                schema: "app",
                table: "DataTable");

            migrationBuilder.DropForeignKey(
                name: "FK_ScdTable_SqlConnection_ConnectionId",
                schema: "app",
                table: "ScdTable");

            migrationBuilder.DropForeignKey(
                name: "FK_SqlConnection_Credential_CredentialId",
                schema: "app",
                table: "SqlConnection");

            migrationBuilder.DropForeignKey(
                name: "FK_Step_SqlConnection_ConnectionId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SqlConnection",
                schema: "app",
                table: "SqlConnection");

            migrationBuilder.RenameTable(
                name: "SqlConnection",
                schema: "app",
                newName: "Connection",
                newSchema: "app");

            migrationBuilder.RenameColumn(
                name: "SqlConnectionType",
                schema: "app",
                table: "Connection",
                newName: "ConnectionType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Connection",
                schema: "app",
                table: "Connection",
                column: "ConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Connection_Credential_CredentialId",
                schema: "app",
                table: "Connection",
                column: "CredentialId",
                principalSchema: "app",
                principalTable: "Credential",
                principalColumn: "CredentialId");

            migrationBuilder.AddForeignKey(
                name: "FK_DataTable_Connection_ConnectionId",
                schema: "app",
                table: "DataTable",
                column: "ConnectionId",
                principalSchema: "app",
                principalTable: "Connection",
                principalColumn: "ConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScdTable_Connection_ConnectionId",
                schema: "app",
                table: "ScdTable",
                column: "ConnectionId",
                principalSchema: "app",
                principalTable: "Connection",
                principalColumn: "ConnectionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Step_Connection_ConnectionId",
                schema: "app",
                table: "Step",
                column: "ConnectionId",
                principalSchema: "app",
                principalTable: "Connection",
                principalColumn: "ConnectionId");
        }
    }
}
