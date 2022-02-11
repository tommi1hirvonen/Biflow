namespace Biflow.Scheduler.Core;

public class SchedulerSchedule
{
    public Guid ScheduleId { get; }

    public Guid JobId { get; }

    public string CronExpression { get; }

    public SchedulerSchedule(Guid scheduleId, Guid jobId, string cronExpression)
    {
        ScheduleId = scheduleId;
        JobId = jobId;
        CronExpression = cronExpression;
    }
}
