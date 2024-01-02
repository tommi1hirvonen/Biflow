using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class SubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasDiscriminator<SubscriptionType>("SubscriptionType")
            .HasValue<JobSubscription>(SubscriptionType.Job)
            .HasValue<JobTagSubscription>(SubscriptionType.JobTag)
            .HasValue<StepSubscription>(SubscriptionType.Step)
            .HasValue<TagSubscription>(SubscriptionType.Tag);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Subscriptions)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
