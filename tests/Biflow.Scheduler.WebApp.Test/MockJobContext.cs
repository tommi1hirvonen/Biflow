using Quartz;

namespace Biflow.Scheduler.WebApp.Test;

internal class MockJobContext(Guid jobId, Guid scheduleId) : IJobExecutionContext
{
    private readonly MockJobDetail _jobDetail = new(jobId);
    private readonly MockTrigger _mockTrigger = new(scheduleId);

    public IJobDetail JobDetail => _jobDetail;

    public ITrigger Trigger => _mockTrigger;

    #region NotImplemented
    public IScheduler Scheduler => throw new NotImplementedException();

    public ICalendar? Calendar => throw new NotImplementedException();

    public bool Recovering => throw new NotImplementedException();

    public TriggerKey RecoveringTriggerKey => throw new NotImplementedException();

    public int RefireCount => throw new NotImplementedException();

    public JobDataMap MergedJobDataMap => throw new NotImplementedException();

    public IJob JobInstance => throw new NotImplementedException();

    public DateTimeOffset FireTimeUtc => throw new NotImplementedException();

    public DateTimeOffset? ScheduledFireTimeUtc => throw new NotImplementedException();

    public DateTimeOffset? PreviousFireTimeUtc => throw new NotImplementedException();

    public DateTimeOffset? NextFireTimeUtc => throw new NotImplementedException();

    public string FireInstanceId => throw new NotImplementedException();

    public object? Result { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public TimeSpan JobRunTime => throw new NotImplementedException();

    public CancellationToken CancellationToken => throw new NotImplementedException();

    public object? Get(object key)
    {
        throw new NotImplementedException();
    }

    public void Put(object key, object objectValue)
    {
        throw new NotImplementedException();
    }
    #endregion
}
