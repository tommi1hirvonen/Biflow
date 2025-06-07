using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ScdTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ApplyIndexesOnCreate",
                schema: "app",
                table: "ScdTable",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SelectDistinct",
                schema: "app",
                table: "ScdTable",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplyIndexesOnCreate",
                schema: "app",
                table: "ScdTable");

            migrationBuilder.DropColumn(
                name: "SelectDistinct",
                schema: "app",
                table: "ScdTable");
        }
    }
}
