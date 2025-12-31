using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PropertyTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyTranslationSet",
                schema: "app",
                columns: table => new
                {
                    PropertyTranslationSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyTranslationSetName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTranslationSet", x => x.PropertyTranslationSetId);
                });

            migrationBuilder.CreateTable(
                name: "PropertyTranslation",
                schema: "app",
                columns: table => new
                {
                    PropertyTranslationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyTranslationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    PropertyPaths = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExactMatch = table.Column<bool>(type: "bit", nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PropertyTranslationSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTranslation", x => x.PropertyTranslationId);
                    table.ForeignKey(
                        name: "FK_PropertyTranslation_PropertyTranslationSet_PropertyTranslationSetId",
                        column: x => x.PropertyTranslationSetId,
                        principalSchema: "app",
                        principalTable: "PropertyTranslationSet",
                        principalColumn: "PropertyTranslationSetId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyTranslation",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PropertyTranslationSet",
                schema: "app");
        }
    }
}
