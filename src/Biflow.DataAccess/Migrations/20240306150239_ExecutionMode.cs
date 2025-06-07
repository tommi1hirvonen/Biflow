using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ExecutionMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExecutionMode",
                schema: "app",
                table: "Job",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "ExecutionPhase");

            migrationBuilder.AddColumn<string>(
                name: "ExecutionMode",
                schema: "app",
                table: "Execution",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "ExecutionPhase");

            migrationBuilder.Sql("""
                UPDATE [app].[Job]
                SET [ExecutionMode] = CASE WHEN [UseDependencyMode] = 1 THEN 'Dependency' ELSE 'ExecutionPhase' END
                """);

            migrationBuilder.Sql("""
                UPDATE [app].[Execution]
                SET [ExecutionMode] = CASE WHEN [DependencyMode] = 1 THEN 'Dependency' ELSE 'ExecutionPhase' END
                """);

            migrationBuilder.DropColumn(
                name: "UseDependencyMode",
                schema: "app",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "DependencyMode",
                schema: "app",
                table: "Execution");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseDependencyMode",
                schema: "app",
                table: "Job",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DependencyMode",
                schema: "app",
                table: "Execution",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE [app].[Job]
                SET [UseDependencyMode] = CASE WHEN [ExecutionMode] = 'Dependency' THEN 1 ELSE 0 END
                """);

            migrationBuilder.Sql("""
                UPDATE [app].[Execution]
                SET [DependencyMode] = CASE WHEN [ExecutionMode] = 'Dependency' THEN 1 ELSE 0 END
                """);

            migrationBuilder.DropColumn(
                name: "ExecutionMode",
                schema: "app",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "ExecutionMode",
                schema: "app",
                table: "Execution");
        }
    }
}
