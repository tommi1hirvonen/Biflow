namespace EtlManager.Executor.Core.ExecutionStopper;

public interface IExecutionStopper
{
    public Task<bool> RunAsync(string executionId, string? username, string? stepId);
}
