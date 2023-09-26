namespace Biflow.Scheduler.Core;

public class SchedulerSchedule
{
    public Guid ScheduleId { get; }

    public Guid JobId { get; }

    public string CronExpression { get; }

    public bool DisallowConcurrentExecution { get; }

    public SchedulerSchedule(Guid scheduleId, Guid jobId, string cronExpression, bool disallowConcurrentExecution)
    {
        ScheduleId = scheduleId;
        JobId = jobId;
        CronExpression = cronExpression;
        DisallowConcurrentExecution = disallowConcurrentExecution;
    }
}
