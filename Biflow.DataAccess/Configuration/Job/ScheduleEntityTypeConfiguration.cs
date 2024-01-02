using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class ScheduleEntityTypeConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.HasOne(schedule => schedule.Job)
            .WithMany(job => job.Schedules)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.JobId, x.CronExpression }, "UQ_Schedule")
            .IsUnique();
    }
}
