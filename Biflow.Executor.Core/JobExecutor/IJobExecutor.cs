namespace Biflow.Executor.Core.JobExecutor;

public interface IJobExecutor
{
    public void Cancel(string username);

    public void Cancel(string username, Guid stepId);

    Task RunAsync(Guid executionId);
}
