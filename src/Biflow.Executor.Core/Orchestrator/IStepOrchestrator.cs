namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepOrchestrator
{
    public Task<bool> RunAsync(OrchestrationContext context, StepExecution stepExecution,
        CancellationContext cancellationContext);
}