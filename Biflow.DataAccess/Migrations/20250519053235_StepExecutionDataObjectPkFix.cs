using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class StepExecutionDataObjectPkFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ExecutionStepDataObject",
                schema: "app",
                table: "ExecutionStepDataObject");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExecutionStepDataObject",
                schema: "app",
                table: "ExecutionStepDataObject",
                columns: new[] { "ExecutionId", "StepId", "ObjectId", "ReferenceType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ExecutionStepDataObject",
                schema: "app",
                table: "ExecutionStepDataObject");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExecutionStepDataObject",
                schema: "app",
                table: "ExecutionStepDataObject",
                columns: new[] { "ExecutionId", "StepId", "ObjectId" });
        }
    }
}
