namespace Biflow.Executor.Core.StepExecutor;

internal interface IStepExecutor : IDisposable
{
    public Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts);
}
