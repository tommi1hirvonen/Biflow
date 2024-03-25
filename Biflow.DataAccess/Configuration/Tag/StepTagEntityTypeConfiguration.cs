namespace Biflow.DataAccess.Configuration;

internal class StepTagEntityTypeConfiguration : IEntityTypeConfiguration<StepTag>
{
    public void Configure(EntityTypeBuilder<StepTag> builder)
    {
        builder.HasMany(t => t.Steps)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("StepTag",
            x => x.HasOne<Step>().WithMany().HasForeignKey("StepId").HasPrincipalKey(y => y.StepId).OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<StepTag>().WithMany().HasForeignKey("TagId").HasPrincipalKey(y => y.TagId).OnDelete(DeleteBehavior.Cascade));

        builder.HasMany(t => t.JobSteps)
            .WithMany(s => s.TagFilters)
            .UsingEntity<Dictionary<string, object>>("JobStepTagFilter",
            x => x.HasOne<JobStep>().WithMany().HasForeignKey("StepId").HasPrincipalKey(y => y.StepId).OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<StepTag>().WithMany().HasForeignKey("TagId").HasPrincipalKey(y => y.TagId).OnDelete(DeleteBehavior.Cascade));

        builder.HasMany(t => t.Schedules)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("ScheduleTag",
            x => x.HasOne<Schedule>().WithMany().HasForeignKey("ScheduleId").HasPrincipalKey(y => y.ScheduleId).OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<StepTag>().WithMany().HasForeignKey("TagId").HasPrincipalKey(y => y.TagId).OnDelete(DeleteBehavior.Cascade));
    }
}
