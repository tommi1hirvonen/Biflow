using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ClusterConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClusterConfiguration",
                schema: "app",
                table: "Step",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClusterConfiguration",
                schema: "app",
                table: "ExecutionStep",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClusterConfiguration",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "ClusterConfiguration",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
