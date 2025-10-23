namespace Biflow.Executor.Core.Orchestrator;

public interface IStepOrchestrator
{
    public Task<bool> RunAsync(OrchestrationContext context, StepExecution stepExecution,
        ExtendedCancellationTokenSource cts);
}