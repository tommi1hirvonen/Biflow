namespace Biflow.Executor.Core.JobExecutor;

public interface IJobExecutor
{
    public Execution Execution { get; }

    public void Cancel(string username);

    public void Cancel(string username, Guid stepId);

    public Task RunAsync(CancellationToken cancellationToken);
}
