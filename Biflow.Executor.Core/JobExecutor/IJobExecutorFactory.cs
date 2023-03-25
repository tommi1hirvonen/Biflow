namespace Biflow.Executor.Core.JobExecutor;

public interface IJobExecutorFactory
{
    public Task<IJobExecutor> CreateAsync(Guid executionId);
}