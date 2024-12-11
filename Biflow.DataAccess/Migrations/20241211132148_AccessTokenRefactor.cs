using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AccessTokenRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessToken",
                schema: "app",
                table: "AccessToken");

            migrationBuilder.DropColumn(
                name: "Username",
                schema: "app",
                table: "AccessToken");

            migrationBuilder.AlterColumn<string>(
                name: "ResourceUrl",
                schema: "app",
                table: "AccessToken",
                type: "varchar(850)",
                unicode: false,
                maxLength: 850,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldUnicode: false,
                oldMaxLength: 1000);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessToken",
                schema: "app",
                table: "AccessToken",
                columns: new[] { "AzureCredentialId", "ResourceUrl" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessToken",
                schema: "app",
                table: "AccessToken");

            migrationBuilder.AlterColumn<string>(
                name: "ResourceUrl",
                schema: "app",
                table: "AccessToken",
                type: "varchar(1000)",
                unicode: false,
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(850)",
                oldUnicode: false,
                oldMaxLength: 850);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                schema: "app",
                table: "AccessToken",
                type: "varchar(256)",
                unicode: false,
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessToken",
                schema: "app",
                table: "AccessToken",
                columns: new[] { "AzureCredentialId", "Username", "ResourceUrl" });
        }
    }
}
