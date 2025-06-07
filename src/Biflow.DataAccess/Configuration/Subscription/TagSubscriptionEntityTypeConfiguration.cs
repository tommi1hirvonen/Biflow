namespace Biflow.DataAccess.Configuration;

internal class TagSubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<StepTagSubscription>
{
    public void Configure(EntityTypeBuilder<StepTagSubscription> builder)
    {
        builder.Property(x => x.AlertType).HasColumnName("AlertType");
        builder.Property(x => x.TagId).HasColumnName("TagId");

        builder.HasOne(x => x.Tag)
            .WithMany(x => x.StepTagSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.TagId }, "IX_UQ_Subscription_StepTagSubscription")
            .HasFilter($"[{nameof(Subscription.SubscriptionType)}] = '{nameof(SubscriptionType.StepTag)}'")
            .IsUnique();
    }
}
