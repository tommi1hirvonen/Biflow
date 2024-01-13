using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class TagSubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<TagSubscription>
{
    public void Configure(EntityTypeBuilder<TagSubscription> builder)
    {
        builder.HasOne(x => x.Tag)
            .WithMany(x => x.TagSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.TagId }, "IX_UQ_Subscription_TagSubscription")
            .HasFilter($"[{nameof(Subscription.SubscriptionType)}] = '{nameof(SubscriptionType.Tag)}'")
            .IsUnique();
    }
}
