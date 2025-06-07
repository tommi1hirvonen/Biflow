namespace Biflow.DataAccess.Configuration;

internal class SubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscription")
            .HasKey(x => x.SubscriptionId);

        builder.HasDiscriminator<SubscriptionType>("SubscriptionType")
            .HasValue<JobSubscription>(SubscriptionType.Job)
            .HasValue<JobStepTagSubscription>(SubscriptionType.JobStepTag)
            .HasValue<StepSubscription>(SubscriptionType.Step)
            .HasValue<StepTagSubscription>(SubscriptionType.StepTag);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Subscriptions)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
