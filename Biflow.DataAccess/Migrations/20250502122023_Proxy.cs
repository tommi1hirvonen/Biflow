using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Proxy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProxyId",
                schema: "app",
                table: "Step",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProxyId",
                schema: "app",
                table: "ExecutionStep",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Proxy",
                schema: "app",
                columns: table => new
                {
                    ProxyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProxyName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ProxyUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proxy", x => x.ProxyId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Step_Proxy_ProxyId",
                schema: "app",
                table: "Step",
                column: "ProxyId",
                principalSchema: "app",
                principalTable: "Proxy",
                principalColumn: "ProxyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Step_Proxy_ProxyId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropTable(
                name: "Proxy",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "ProxyId",
                schema: "app",
                table: "Step");

            migrationBuilder.DropColumn(
                name: "ProxyId",
                schema: "app",
                table: "ExecutionStep");
        }
    }
}
