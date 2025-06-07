using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ScdTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ScdTableId",
                schema: "app",
                table: "Step",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScdTableId",
                schema: "app",
                table: "ExecutionStep",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScdTable",
                schema: "app",
                columns: table => new
                {
                    ScdTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScdTableName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SourceTableSchema = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceTableName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TargetTableSchema = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TargetTableName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StagingTableSchema = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StagingTableName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PreLoadScript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostLoadScript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullLoad = table.Column<bool>(type: "bit", nullable: false),
                    NaturalKeyColumns = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: false),
                    SchemaDriftConfiguration = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScdTable", x => x.ScdTableId);
                    table.ForeignKey(
                        name: "FK_ScdTable_Connection_ConnectionId",
                        column: x => x.ConnectionId,
                        principalSchema: "app",
                        principalTable: "Connection",
                        principalColumn: "ConnectionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Step_ScdTable_ScdTableId",
                schema: "app",
                table: "Step",
                column: "ScdTableId",
                principalSchema: "app",
                principalTable: "ScdTable",
                principalColumn: "ScdTableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Step_ScdTable_ScdTableId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropTable(
                name: "ScdTable",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "ScdTableId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "ScdTableId",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
