namespace Biflow.Executor.Core.StepExecutor;

internal interface IStepExecutorProvider
{
    public IStepExecutor GetExecutorFor(StepExecution step, StepExecutionAttempt attempt);
}