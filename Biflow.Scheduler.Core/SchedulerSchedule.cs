using Biflow.DataAccess.Models;

namespace Biflow.Scheduler.Core;

public record SchedulerSchedule(Guid ScheduleId, Guid JobId, string CronExpression, bool DisallowConcurrentExecution, bool IsEnabled)
{
    public static SchedulerSchedule From(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule.CronExpression);
        return new(schedule.ScheduleId, schedule.JobId, schedule.CronExpression, schedule.DisallowConcurrentExecution, schedule.IsEnabled);
    }
}
