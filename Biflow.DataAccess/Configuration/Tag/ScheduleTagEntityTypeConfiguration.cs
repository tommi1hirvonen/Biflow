namespace Biflow.DataAccess.Configuration;

internal class ScheduleTagEntityTypeConfiguration : IEntityTypeConfiguration<ScheduleTag>
{
    public void Configure(EntityTypeBuilder<ScheduleTag> builder)
    {
        builder.HasMany(t => t.Schedules)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("ScheduleTag",
            x => x.HasOne<Schedule>().WithMany().HasForeignKey("ScheduleId").HasPrincipalKey(y => y.ScheduleId).OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<ScheduleTag>().WithMany().HasForeignKey("TagId").HasPrincipalKey(y => y.TagId).OnDelete(DeleteBehavior.Cascade));
    }
}
