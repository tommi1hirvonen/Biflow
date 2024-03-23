using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ExecutionForeignKeyChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionDependency_ExecutionStep_ExecutionId_StepId",
                schema: "app",
                table: "ExecutionDependency");

            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionParameter_Execution_ExecutionId",
                schema: "app",
                table: "ExecutionParameter");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionDependency_ExecutionStep_ExecutionId_StepId",
                schema: "app",
                table: "ExecutionDependency",
                columns: new[] { "ExecutionId", "StepId" },
                principalSchema: "app",
                principalTable: "ExecutionStep",
                principalColumns: new[] { "ExecutionId", "StepId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionParameter_Execution_ExecutionId",
                schema: "app",
                table: "ExecutionParameter",
                column: "ExecutionId",
                principalSchema: "app",
                principalTable: "Execution",
                principalColumn: "ExecutionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionDependency_ExecutionStep_ExecutionId_StepId",
                schema: "app",
                table: "ExecutionDependency");

            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionParameter_Execution_ExecutionId",
                schema: "app",
                table: "ExecutionParameter");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionDependency_ExecutionStep_ExecutionId_StepId",
                schema: "app",
                table: "ExecutionDependency",
                columns: new[] { "ExecutionId", "StepId" },
                principalSchema: "app",
                principalTable: "ExecutionStep",
                principalColumns: new[] { "ExecutionId", "StepId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionParameter_Execution_ExecutionId",
                schema: "app",
                table: "ExecutionParameter",
                column: "ExecutionId",
                principalSchema: "app",
                principalTable: "Execution",
                principalColumn: "ExecutionId");
        }
    }
}
