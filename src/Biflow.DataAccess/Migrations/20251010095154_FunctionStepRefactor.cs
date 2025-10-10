using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FunctionStepRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FunctionIsDurable",
                schema: "app",
                table: "Step",
                newName: "FunctionDisableAsyncPattern");

            migrationBuilder.RenameColumn(
                name: "FunctionIsDurable",
                schema: "app",
                table: "ExecutionStep",
                newName: "FunctionDisableAsyncPattern");
            
            // Since the purpose and meaning of the property has changed somewhat, reset it to false.
            // This is the safer option.
            migrationBuilder.Sql("""
                UPDATE app.Step
                SET FunctionDisableAsyncPattern = 0
                WHERE StepType = 'Function'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FunctionDisableAsyncPattern",
                schema: "app",
                table: "Step",
                newName: "FunctionIsDurable");

            migrationBuilder.RenameColumn(
                name: "FunctionDisableAsyncPattern",
                schema: "app",
                table: "ExecutionStep",
                newName: "FunctionIsDurable");
            
            migrationBuilder.Sql("""
                UPDATE app.Step
                SET FunctionIsDurable = 0
                WHERE StepType = 'Function'
                """);
        }
    }
}
