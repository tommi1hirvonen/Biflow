using Biflow.DataAccess.Models;

namespace Biflow.Scheduler.Core;

public record SchedulerSchedule(Guid ScheduleId, Guid JobId, string CronExpression, bool DisallowConcurrentExecution, bool IsEnabled)
{
    public SchedulerSchedule(Schedule schedule)
        : this(schedule.ScheduleId, schedule.JobId, schedule.CronExpression, schedule.DisallowConcurrentExecution, schedule.IsEnabled) { }
}
