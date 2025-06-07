using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ManagedIdentityCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: "app",
                table: "AzureCredential",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                schema: "app",
                table: "AzureCredential",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: "app",
                table: "AzureCredential",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                schema: "app",
                table: "AzureCredential",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36,
                oldNullable: true);
        }
    }
}
