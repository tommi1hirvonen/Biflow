using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal interface IStepExecutor<TStep, TAttempt> : IStepExecutor
    where TStep : StepExecution
    where TAttempt : StepExecutionAttempt;

internal interface IStepExecutor
{
    public Task<bool> RunAsync(OrchestrationContext context, StepExecution stepExecution,
        ExtendedCancellationTokenSource cts);
}
