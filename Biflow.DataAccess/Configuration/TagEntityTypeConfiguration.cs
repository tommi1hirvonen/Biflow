namespace Biflow.DataAccess.Configuration;

internal class TagEntityTypeConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tag")
            .HasKey(x => x.TagId);

        builder.HasMany(t => t.Steps)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("StepTag",
            x => x.HasOne<Step>().WithMany().HasForeignKey("StepId").OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade));

        builder.HasMany(t => t.JobSteps)
            .WithMany(s => s.TagFilters)
            .UsingEntity<Dictionary<string, object>>("JobStepTagFilter",
            x => x.HasOne<JobStep>().WithMany().HasForeignKey("StepId").OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade));

        builder.HasMany(t => t.Schedules)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("ScheduleTag",
            x => x.HasOne<Schedule>().WithMany().HasForeignKey("ScheduleId").OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade));

        builder.HasIndex(p => p.TagName, "UQ_TagName").IsUnique();
    }
}
