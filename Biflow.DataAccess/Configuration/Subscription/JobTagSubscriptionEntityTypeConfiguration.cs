namespace Biflow.DataAccess.Configuration;

internal class JobTagSubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<JobStepTagSubscription>
{
    public void Configure(EntityTypeBuilder<JobStepTagSubscription> builder)
    {
        builder.Property(x => x.AlertType).HasColumnName("AlertType");
        builder.Property(x => x.JobId).HasColumnName("JobId");
        builder.Property(x => x.TagId).HasColumnName("TagId");

        builder.HasOne(x => x.Job)
            .WithMany(x => x.JobStepTagSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tag)
            .WithMany(x => x.JobStepTagSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.JobId, x.TagId }, "IX_UQ_Subscription_JobStepTagSubscription")
            .HasFilter($"[{nameof(Subscription.SubscriptionType)}] = '{nameof(SubscriptionType.JobStepTag)}'")
            .IsUnique();
    }
}
