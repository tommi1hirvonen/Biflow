using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AzureCredentialRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessToken_AppRegistration_AppRegistrationId",
                schema: "app",
                table: "AccessToken");

            migrationBuilder.DropForeignKey(
                name: "FK_BlobStorageClient_AppRegistration_AppRegistrationId",
                schema: "app",
                table: "BlobStorageClient");

            migrationBuilder.DropForeignKey(
                name: "FK_FunctionApp_AppRegistration_AppRegistrationId",
                schema: "app",
                table: "FunctionApp");

            migrationBuilder.DropForeignKey(
                name: "FK_PipelineClient_AppRegistration_AppRegistrationId",
                schema: "app",
                table: "PipelineClient");

            migrationBuilder.DropForeignKey(
                name: "FK_Step_AppRegistration_AppRegistrationId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessToken",
                schema: "app",
                table: "AccessToken");

            migrationBuilder.RenameColumn(
                name: "AppRegistrationId",
                schema: "app",
                table: "Step",
                newName: "AzureCredentialId");

            migrationBuilder.RenameColumn(
                name: "AppRegistrationId",
                schema: "app",
                table: "PipelineClient",
                newName: "AzureCredentialId");

            migrationBuilder.RenameColumn(
                name: "AppRegistrationId",
                schema: "app",
                table: "FunctionApp",
                newName: "AzureCredentialId");

            migrationBuilder.RenameColumn(
                name: "AppRegistrationId",
                schema: "app",
                table: "ExecutionStep",
                newName: "AzureCredentialId");

            migrationBuilder.RenameColumn(
                name: "AppRegistrationId",
                schema: "app",
                table: "BlobStorageClient",
                newName: "AzureCredentialId");

            migrationBuilder.RenameColumn(
                name: "AppRegistrationId",
                schema: "app",
                table: "AccessToken",
                newName: "AzureCredentialId");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                schema: "app",
                table: "AccessToken",
                type: "varchar(256)",
                unicode: false,
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessToken",
                schema: "app",
                table: "AccessToken",
                columns: new[] { "AzureCredentialId", "Username", "ResourceUrl" });

            migrationBuilder.CreateTable(
                name: "AzureCredential",
                schema: "app",
                columns: table => new
                {
                    AzureCredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AzureCredentialName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AzureCredentialType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "ServicePrincipal"),
                    TenantId = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    ClientId = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ClientSecret = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzureCredential", x => x.AzureCredentialId);
                });

            migrationBuilder.Sql("""
                insert into app.AzureCredential (
                    AzureCredentialId,
                    AzureCredentialName,
                    AzureCredentialType,
                    TenantId,
                    ClientId,
                    ClientSecret
                )
                select 
                    AzureCredentialId = AppRegistrationId,
                    AzureCredentialName = AppRegistrationName,
                    AzureCredentialType = 'ServicePrincipal',
                    TenantId,
                    ClientId,
                    ClientSecret
                from app.AppRegistration
                """);
            
            migrationBuilder.DropTable(
                name: "AppRegistration",
                schema: "app");

            migrationBuilder.AddForeignKey(
                name: "FK_AccessToken_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "AccessToken",
                column: "AzureCredentialId",
                principalSchema: "app",
                principalTable: "AzureCredential",
                principalColumn: "AzureCredentialId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BlobStorageClient_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "BlobStorageClient",
                column: "AzureCredentialId",
                principalSchema: "app",
                principalTable: "AzureCredential",
                principalColumn: "AzureCredentialId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FunctionApp_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "FunctionApp",
                column: "AzureCredentialId",
                principalSchema: "app",
                principalTable: "AzureCredential",
                principalColumn: "AzureCredentialId");

            migrationBuilder.AddForeignKey(
                name: "FK_PipelineClient_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "PipelineClient",
                column: "AzureCredentialId",
                principalSchema: "app",
                principalTable: "AzureCredential",
                principalColumn: "AzureCredentialId");

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
                name: "FK_AccessToken_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "AccessToken");

            migrationBuilder.DropForeignKey(
                name: "FK_BlobStorageClient_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "BlobStorageClient");

            migrationBuilder.DropForeignKey(
                name: "FK_FunctionApp_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "FunctionApp");

            migrationBuilder.DropForeignKey(
                name: "FK_PipelineClient_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "PipelineClient");

            migrationBuilder.DropForeignKey(
                name: "FK_Step_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropTable(
                name: "AzureCredential",
                schema: "app");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessToken",
                schema: "app",
                table: "AccessToken");

            migrationBuilder.DropColumn(
                name: "Username",
                schema: "app",
                table: "AccessToken");

            migrationBuilder.RenameColumn(
                name: "AzureCredentialId",
                schema: "app",
                table: "Step",
                newName: "AppRegistrationId");

            migrationBuilder.RenameColumn(
                name: "AzureCredentialId",
                schema: "app",
                table: "PipelineClient",
                newName: "AppRegistrationId");

            migrationBuilder.RenameColumn(
                name: "AzureCredentialId",
                schema: "app",
                table: "FunctionApp",
                newName: "AppRegistrationId");

            migrationBuilder.RenameColumn(
                name: "AzureCredentialId",
                schema: "app",
                table: "ExecutionStep",
                newName: "AppRegistrationId");

            migrationBuilder.RenameColumn(
                name: "AzureCredentialId",
                schema: "app",
                table: "BlobStorageClient",
                newName: "AppRegistrationId");

            migrationBuilder.RenameColumn(
                name: "AzureCredentialId",
                schema: "app",
                table: "AccessToken",
                newName: "AppRegistrationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessToken",
                schema: "app",
                table: "AccessToken",
                columns: new[] { "AppRegistrationId", "ResourceUrl" });

            migrationBuilder.CreateTable(
                name: "AppRegistration",
                schema: "app",
                columns: table => new
                {
                    AppRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppRegistrationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ClientId = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    ClientSecret = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: false),
                    TenantId = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRegistration", x => x.AppRegistrationId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_AccessToken_AppRegistration_AppRegistrationId",
                schema: "app",
                table: "AccessToken",
                column: "AppRegistrationId",
                principalSchema: "app",
                principalTable: "AppRegistration",
                principalColumn: "AppRegistrationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BlobStorageClient_AppRegistration_AppRegistrationId",
                schema: "app",
                table: "BlobStorageClient",
                column: "AppRegistrationId",
                principalSchema: "app",
                principalTable: "AppRegistration",
                principalColumn: "AppRegistrationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FunctionApp_AppRegistration_AppRegistrationId",
                schema: "app",
                table: "FunctionApp",
                column: "AppRegistrationId",
                principalSchema: "app",
                principalTable: "AppRegistration",
                principalColumn: "AppRegistrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PipelineClient_AppRegistration_AppRegistrationId",
                schema: "app",
                table: "PipelineClient",
                column: "AppRegistrationId",
                principalSchema: "app",
                principalTable: "AppRegistration",
                principalColumn: "AppRegistrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Step_AppRegistration_AppRegistrationId",
                schema: "app",
                table: "Step",
                column: "AppRegistrationId",
                principalSchema: "app",
                principalTable: "AppRegistration",
                principalColumn: "AppRegistrationId");
        }
    }
}
