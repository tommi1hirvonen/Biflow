namespace Biflow.Executor.ConsoleApp.ExecutionStopper;

public interface IExecutionStopper
{
    public Task<bool> RunAsync(string executionId, string? username, string? stepId);
}
