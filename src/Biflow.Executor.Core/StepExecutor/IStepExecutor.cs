namespace Biflow.Executor.Core.StepExecutor;

internal interface IStepExecutor
{
    public Task<Result> ExecuteAsync(OrchestrationContext orchestrationContext,
        CancellationContext cancellationContext);
}
