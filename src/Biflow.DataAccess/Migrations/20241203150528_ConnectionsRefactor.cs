using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ConnectionsRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AnalysisServicesConnectionId",
                schema: "app",
                table: "Step",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AnalysisServicesConnectionId",
                schema: "app",
                table: "ExecutionStep",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnalysisServicesConnection",
                schema: "app",
                columns: table => new
                {
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConnectionString = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisServicesConnection", x => x.ConnectionId);
                    table.ForeignKey(
                        name: "FK_AnalysisServicesConnection_Credential_CredentialId",
                        column: x => x.CredentialId,
                        principalSchema: "app",
                        principalTable: "Credential",
                        principalColumn: "CredentialId");
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Step_AnalysisServicesConnection_AnalysisServicesConnectionId",
                schema: "app",
                table: "Step",
                column: "AnalysisServicesConnectionId",
                principalSchema: "app",
                principalTable: "AnalysisServicesConnection",
                principalColumn: "ConnectionId");

            migrationBuilder.Sql("""
                insert into [app].[AnalysisServicesConnection] (
                   [ConnectionId],
                   [ConnectionName],
                   [CredentialId],
                   [ConnectionString]
                )
                select
                   [ConnectionId],
                   [ConnectionName],
                   [CredentialId],
                   [ConnectionString]
                from [app].[Connection]
                where [ConnectionType] = 'AnalysisServices'
                
                update a 
                set AnalysisServicesConnectionId = ConnectionId, ConnectionId = NULL
                from [app].[Step] as a 
                where a.[StepType] = 'Tabular'
                
                update a 
                set AnalysisServicesConnectionId = ConnectionId, ConnectionId = NULL
                from [app].[ExecutionStep] as a 
                where a.[StepType] = 'Tabular'
                
                delete from a
                from [app].[Connection] as a 
                where [ConnectionType] = 'AnalysisServices'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Step_AnalysisServicesConnection_AnalysisServicesConnectionId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropTable(
                name: "AnalysisServicesConnection",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "AnalysisServicesConnectionId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "AnalysisServicesConnectionId",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
