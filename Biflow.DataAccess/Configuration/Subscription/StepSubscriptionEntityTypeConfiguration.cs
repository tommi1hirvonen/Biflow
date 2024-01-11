using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class StepSubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<StepSubscription>
{
    public void Configure(EntityTypeBuilder<StepSubscription> builder)
    {
        builder.HasOne(x => x.Step)
            .WithMany(x => x.StepSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.StepId }, "IX_UQ_Subscription_StepSubscription")
            .HasFilter($"[{nameof(Subscription.SubscriptionType)}] = '{nameof(SubscriptionType.Step)}'")
            .IsUnique();
    }
}
