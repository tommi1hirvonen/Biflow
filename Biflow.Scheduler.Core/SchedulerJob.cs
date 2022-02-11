namespace Biflow.Scheduler.Core;

public class SchedulerJob
{
    public Guid JobId { get; }

    public SchedulerJob(Guid jobId)
    {
        JobId = jobId;
    }
}
