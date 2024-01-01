namespace Biflow.Executor.Core.JobExecutor;

public class ExecutionNotFoundException(Guid executionId) : Exception($"No execution found for id {executionId}")
{
    public Guid ExecutionId { get; } = executionId;
}