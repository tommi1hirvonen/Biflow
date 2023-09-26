namespace Biflow.Executor.Core.JobExecutor;

public interface IJobExecutor
{
    public void Cancel(string username);

    public void Cancel(string username, Guid stepId);

    public Task RunAsync(Guid executionId, CancellationToken cancellationToken);
}
