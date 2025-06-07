using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class JobTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_TagName",
                schema: "app",
                table: "Tag");

            migrationBuilder.AddColumn<string>(
                name: "TagType",
                schema: "app",
                table: "Tag",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "Step");

            migrationBuilder.CreateTable(
                name: "JobTag",
                schema: "app",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTag", x => new { x.JobId, x.TagId });
                    table.ForeignKey(
                        name: "FK_JobTag_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "app",
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobTag_Tag_TagId",
                        column: x => x.TagId,
                        principalSchema: "app",
                        principalTable: "Tag",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_TagName",
                schema: "app",
                table: "Tag",
                columns: new[] { "TagName", "TagType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobTag",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "UQ_TagName",
                schema: "app",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "TagType",
                schema: "app",
                table: "Tag");

            migrationBuilder.CreateIndex(
                name: "UQ_TagName",
                schema: "app",
                table: "Tag",
                column: "TagName",
                unique: true);
        }
    }
}
