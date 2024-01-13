using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class JobTagSubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<JobTagSubscription>
{
    public void Configure(EntityTypeBuilder<JobTagSubscription> builder)
    {
        builder.HasOne(x => x.Job)
            .WithMany(x => x.JobTagSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tag)
            .WithMany(x => x.JobTagSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.JobId, x.TagId }, "IX_UQ_Subscription_JobTagSubscription")
            .HasFilter($"[{nameof(Subscription.SubscriptionType)}] = '{nameof(SubscriptionType.JobTag)}'")
            .IsUnique();
    }
}
