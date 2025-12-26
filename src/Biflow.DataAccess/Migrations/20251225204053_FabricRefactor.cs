using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FabricRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FabricWorkspace",
                schema: "app",
                columns: table => new
                {
                    FabricWorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FabricWorkspaceName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AzureCredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FabricWorkspace", x => x.FabricWorkspaceId);
                    table.ForeignKey(
                        name: "FK_FabricWorkspace_AzureCredential_AzureCredentialId",
                        column: x => x.AzureCredentialId,
                        principalSchema: "app",
                        principalTable: "AzureCredential",
                        principalColumn: "AzureCredentialId");
                });

            // Migrate Fabric/Power BI workspace ids to the new FabricWorkspace table.
            migrationBuilder.Sql(
                """
                insert into app.FabricWorkspace (FabricWorkspaceId, FabricWorkspaceName, WorkspaceId, AzureCredentialId)
                select
                	newid() as FabricWorkspaceId,
                	isnull(max(WorkspaceName), concat('Workspace ', row_number() over (order by (select null)))) as FabricWorkspaceName,
                	WorkspaceId,
                	AzureCredentialId
                from (
                	select cast(DataflowGroupId as uniqueidentifier) as WorkspaceId,
                		DataflowGroupName as WorkspaceName,
                		AzureCredentialId
                	from app.Step
                	where DataflowGroupId is not null
                	union
                	select cast(DatasetGroupId as uniqueidentifier),
                		DatasetGroupName,
                		AzureCredentialId
                	from app.Step
                	where DatasetGroupId is not null
                	union
                	select FabricWorkspaceId,
                		FabricWorkspaceName,
                		AzureCredentialId
                	from app.Step
                	where FabricWorkspaceId is not null
                	) as a
                group by
                	WorkspaceId,
                	AzureCredentialId
                """);
            
            // Update the FabricWorkspaceId for Dataset, Dataflow and Fabric steps.
            
            // Dataset steps
            migrationBuilder.Sql(
                """
                update a
                set FabricWorkspaceId = b.FabricWorkspaceId
                from app.Step as a
                	inner join app.FabricWorkspace as b on
                		cast(a.DatasetGroupId as uniqueidentifier) = b.WorkspaceId and
                		a.AzureCredentialId = b.AzureCredentialId
                """);
            
            // Dataflow steps
            migrationBuilder.Sql(
                """
                update a
                set FabricWorkspaceId = b.FabricWorkspaceId
                from app.Step as a
                	inner join app.FabricWorkspace as b on
                		cast(a.DataflowGroupId as uniqueidentifier) = b.WorkspaceId and
                		a.AzureCredentialId = b.AzureCredentialId
                """);
            
            // Fabric steps
            migrationBuilder.Sql(
                """
                update a
                set FabricWorkspaceId = b.FabricWorkspaceId
                from app.Step as a
                	inner join app.FabricWorkspace as b on
                		a.FabricWorkspaceId = b.WorkspaceId and
                		a.AzureCredentialId = b.AzureCredentialId
                """);
            
            migrationBuilder.DropForeignKey(
                name: "FK_Step_AzureCredential_AzureCredentialId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "AzureCredentialId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DataflowGroupId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DataflowGroupName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DatasetGroupId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "DatasetGroupName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "FabricWorkspaceName",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "AzureCredentialId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DataflowGroupId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DataflowGroupName",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DatasetGroupId",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "DatasetGroupName",
                schema: "app",
                table: "ExecutionStep");

            migrationBuilder.DropColumn(
                name: "FabricWorkspaceName",
                schema: "app",
                table: "ExecutionStep");
            
            migrationBuilder.AddForeignKey(
                name: "FK_Step_FabricWorkspace_FabricWorkspaceId",
                schema: "app",
                table: "Step",
                column: "FabricWorkspaceId",
                principalSchema: "app",
                principalTable: "FabricWorkspace",
                principalColumn: "FabricWorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Step_FabricWorkspace_FabricWorkspaceId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropTable(
                name: "FabricWorkspace",
                schema: "app");

            migrationBuilder.AddColumn<Guid>(
                name: "AzureCredentialId",
                schema: "app",
                table: "Step",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowGroupId",
                schema: "app",
                table: "Step",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowGroupName",
                schema: "app",
                table: "Step",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatasetGroupId",
                schema: "app",
                table: "Step",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatasetGroupName",
                schema: "app",
                table: "Step",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FabricWorkspaceName",
                schema: "app",
                table: "Step",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AzureCredentialId",
                schema: "app",
                table: "ExecutionStep",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowGroupId",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataflowGroupName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatasetGroupId",
                schema: "app",
                table: "ExecutionStep",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatasetGroupName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FabricWorkspaceName",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(max)",
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
    }
}
