using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ExecutionDependencyAlter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionDependency_ExecutionStep_ExecutionId_DependantOnStepId",
                schema: "app",
                table: "ExecutionDependency");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionDependency_ExecutionStep_ExecutionId_DependantOnStepId",
                schema: "app",
                table: "ExecutionDependency",
                columns: new[] { "ExecutionId", "DependantOnStepId" },
                principalSchema: "app",
                principalTable: "ExecutionStep",
                principalColumns: new[] { "ExecutionId", "StepId" });
        }
    }
}
