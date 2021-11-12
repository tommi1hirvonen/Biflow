namespace EtlManager.Executor;

interface IExecutionStopper
{
    public Task<bool> RunAsync(string executionId, string? username, string? stepId);
}
