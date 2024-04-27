using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class StepExecutionMonitor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExecutionStepMonitor",
                schema: "app",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonitoredExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonitoredStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonitoringReason = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionStepMonitor", x => new { x.ExecutionId, x.StepId, x.MonitoredExecutionId, x.MonitoredStepId, x.MonitoringReason });
                    table.ForeignKey(
                        name: "FK_ExecutionStepMonitor_ExecutionStep_ExecutionId_StepId",
                        columns: x => new { x.ExecutionId, x.StepId },
                        principalSchema: "app",
                        principalTable: "ExecutionStep",
                        principalColumns: new[] { "ExecutionId", "StepId" });
                    table.ForeignKey(
                        name: "FK_ExecutionStepMonitor_ExecutionStep_MonitoredExecutionId_MonitoredStepId",
                        columns: x => new { x.MonitoredExecutionId, x.MonitoredStepId },
                        principalSchema: "app",
                        principalTable: "ExecutionStep",
                        principalColumns: new[] { "ExecutionId", "StepId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionStepMonitor_MonitoredExecutionId_MonitoredStepId",
                schema: "app",
                table: "ExecutionStepMonitor",
                columns: new[] { "MonitoredExecutionId", "MonitoredStepId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionStepMonitor",
                schema: "app");
        }
    }
}
