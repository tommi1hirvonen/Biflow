using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJobCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Job_JobCategory_JobCategoryId",
                schema: "app",
                table: "Job");

            migrationBuilder.DropTable(
                name: "JobCategory",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "JobCategoryId",
                schema: "app",
                table: "Job");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "JobCategoryId",
                schema: "app",
                table: "Job",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobCategory",
                schema: "app",
                columns: table => new
                {
                    JobCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobCategoryName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobCategory", x => x.JobCategoryId);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_JobCategory",
                schema: "app",
                table: "JobCategory",
                column: "JobCategoryName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Job_JobCategory_JobCategoryId",
                schema: "app",
                table: "Job",
                column: "JobCategoryId",
                principalSchema: "app",
                principalTable: "JobCategory",
                principalColumn: "JobCategoryId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
