using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class QlikAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Step_QlikCloudClient_QlikCloudClientId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QlikCloudClient",
                table: "QlikCloudClient",
                schema: "app");

            migrationBuilder.RenameTable(
                name: "QlikCloudClient",
                schema: "app",
                newName: "QlikCloudEnvironment",
                newSchema: "app");

            migrationBuilder.RenameColumn(
                name: "QlikCloudClientId",
                table: "QlikCloudEnvironment",
                newName: "QlikCloudEnvironmentId",
                schema: "app");

            migrationBuilder.RenameColumn(
                name: "QlikCloudClientName",
                table: "QlikCloudEnvironment",
                newName: "QlikCloudEnvironmentName",
                schema: "app");

            migrationBuilder.RenameColumn(
                name: "QlikCloudClientId",
                schema: "app",
                table: "Step",
                newName: "QlikCloudEnvironmentId");

            migrationBuilder.RenameColumn(
                name: "QlikCloudClientId",
                schema: "app",
                table: "ExecutionStep",
                newName: "QlikCloudEnvironmentId");

            migrationBuilder.RenameColumn(
                name: "ReloadId",
                schema: "app",
                table: "ExecutionStepAttempt",
                newName: "ReloadOrRunId");

            migrationBuilder.AddColumn<string>(
                name: "QlikStepSettings",
                schema: "app",
                table: "Step",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QlikStepSettings",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [app].[Step]
                SET [QlikStepSettings] = CONCAT('{"$type":"AppReload","AppId":"', [AppId], '"}')
                WHERE [StepType] = 'Qlik'
                """);

            migrationBuilder.Sql("""
                UPDATE [app].[ExecutionStep]
                SET [QlikStepSettings] = CONCAT('{"$type":"AppReload","AppId":"', [AppId], '"}')
                WHERE [StepType] = 'Qlik'
                """);

            migrationBuilder.DropColumn(
                name: "AppId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "AppId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QlikCloudEnvironment",
                table: "QlikCloudEnvironment",
                column: "QlikCloudEnvironmentId",
                schema: "app");

            migrationBuilder.AddForeignKey(
                name: "FK_Step_QlikCloudEnvironment_QlikCloudEnvironmentId",
                schema: "app",
                table: "Step",
                column: "QlikCloudEnvironmentId",
                principalSchema: "app",
                principalTable: "QlikCloudEnvironment",
                principalColumn: "QlikCloudEnvironmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Step_QlikCloudEnvironment_QlikCloudEnvironmentId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropTable(
                name: "QlikCloudEnvironment",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "QlikStepSettings",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "QlikStepSettings",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.RenameColumn(
                name: "QlikCloudEnvironmentId",
                schema: "app",
                table: "Step",
                newName: "QlikCloudClientId");

            migrationBuilder.RenameColumn(
                name: "QlikCloudEnvironmentId",
                schema: "app",
                table: "ExecutionStep",
                newName: "QlikCloudClientId");

            migrationBuilder.AddColumn<string>(
                name: "AppId",
                schema: "app",
                table: "Step",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppId",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QlikCloudClient",
                schema: "app",
                columns: table => new
                {
                    QlikCloudClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    EnvironmentUrl = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    QlikCloudClientName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QlikCloudClient", x => x.QlikCloudClientId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Step_QlikCloudClient_QlikCloudClientId",
                schema: "app",
                table: "Step",
                column: "QlikCloudClientId",
                principalSchema: "app",
                principalTable: "QlikCloudClient",
                principalColumn: "QlikCloudClientId");
        }
    }
}
