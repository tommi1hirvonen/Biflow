using Quartz;

namespace Biflow.Scheduler.WebApp.Test;

internal class MockJobDetail(Guid jobId) : IJobDetail
{
    private readonly JobKey _jobKey = new(jobId.ToString());

    #region NotImplemented
    public JobKey Key => _jobKey;

    public string? Description => throw new NotImplementedException();

    public Type JobType => throw new NotImplementedException();

    public JobDataMap JobDataMap => throw new NotImplementedException();

    public bool Durable => throw new NotImplementedException();

    public bool PersistJobDataAfterExecution => throw new NotImplementedException();

    public bool ConcurrentExecutionDisallowed => throw new NotImplementedException();

    public bool RequestsRecovery => throw new NotImplementedException();

    public IJobDetail Clone()
    {
        throw new NotImplementedException();
    }

    public JobBuilder GetJobBuilder()
    {
        throw new NotImplementedException();
    }
    #endregion
}