using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class TagSubscriptionsRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UQ_Subscription_JobTagSubscription",
                schema: "app",
                table: "Subscription");

            migrationBuilder.DropIndex(
                name: "IX_UQ_Subscription_TagSubscription",
                schema: "app",
                table: "Subscription");

            migrationBuilder.Sql("""
                                 UPDATE [app].[Subscription]
                                 SET [SubscriptionType] = 'StepTag'
                                 WHERE [SubscriptionType] = 'Tag'
                                 """);

            migrationBuilder.Sql("""
                                 UPDATE [app].[Subscription]
                                 SET [SubscriptionType] = 'JobStepTag'
                                 WHERE [SubscriptionType] = 'JobTag'
                                 """);

            migrationBuilder.CreateIndex(
                name: "IX_UQ_Subscription_JobStepTagSubscription",
                schema: "app",
                table: "Subscription",
                columns: new[] { "UserId", "JobId", "TagId" },
                unique: true,
                filter: "[SubscriptionType] = 'JobStepTag'");

            migrationBuilder.CreateIndex(
                name: "IX_UQ_Subscription_StepTagSubscription",
                schema: "app",
                table: "Subscription",
                columns: new[] { "UserId", "TagId" },
                unique: true,
                filter: "[SubscriptionType] = 'StepTag'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UQ_Subscription_JobStepTagSubscription",
                schema: "app",
                table: "Subscription");

            migrationBuilder.DropIndex(
                name: "IX_UQ_Subscription_StepTagSubscription",
                schema: "app",
                table: "Subscription");

            migrationBuilder.CreateIndex(
                name: "IX_UQ_Subscription_JobTagSubscription",
                schema: "app",
                table: "Subscription",
                columns: new[] { "UserId", "JobId", "TagId" },
                unique: true,
                filter: "[SubscriptionType] = 'JobTag'");

            migrationBuilder.CreateIndex(
                name: "IX_UQ_Subscription_TagSubscription",
                schema: "app",
                table: "Subscription",
                columns: new[] { "UserId", "TagId" },
                unique: true,
                filter: "[SubscriptionType] = 'Tag'");
        }
    }
}
