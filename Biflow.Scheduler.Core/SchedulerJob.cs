namespace Biflow.Scheduler.Core;

public class SchedulerJob(Guid jobId)
{
    public Guid JobId { get; } = jobId;
}
