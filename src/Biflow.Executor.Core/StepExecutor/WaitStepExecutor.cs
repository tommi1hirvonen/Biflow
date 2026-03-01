namespace Biflow.Executor.Core.StepExecutor;

internal class WaitStepExecutor(
    WaitStepExecution step,
    WaitStepExecutionAttempt attempt) : IStepExecutor
{
    public async Task<Result> ExecuteAsync(OrchestrationContext context, CancellationContext cancellationContext)
    {
        var cancellationToken = cancellationContext.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(step.WaitSeconds), cancellationToken);
            attempt.AddOutput($"Waited for {step.WaitSeconds} seconds");
            return Result.Success;
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationContext.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }

            attempt.AddError(ex, "Wait operation was canceled unexpectedly");
            return Result.Failure;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error during wait operation");
            return Result.Failure;
        }
    }
}