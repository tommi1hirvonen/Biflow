using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class JobSubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<JobSubscription>
{
    public void Configure(EntityTypeBuilder<JobSubscription> builder)
    {
        builder.HasOne(x => x.Job)
            .WithMany(x => x.JobSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.JobId }, "IX_UQ_Subscription_JobSubscription")
            .HasFilter($"[{nameof(Subscription.SubscriptionType)}] = '{nameof(SubscriptionType.Job)}'")
            .IsUnique();
    }
}
