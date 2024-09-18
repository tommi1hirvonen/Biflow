using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class StepExecutionAttemptIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ExecutionStepAttempt_EndedOn",
                schema: "app",
                table: "ExecutionStepAttempt",
                column: "EndedOn");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionStepAttempt_ExecutionStatus_EndedOn",
                schema: "app",
                table: "ExecutionStepAttempt",
                columns: new[] { "ExecutionStatus", "EndedOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExecutionStepAttempt_EndedOn",
                schema: "app",
                table: "ExecutionStepAttempt");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionStepAttempt_ExecutionStatus_EndedOn",
                schema: "app",
                table: "ExecutionStepAttempt");
        }
    }
}
