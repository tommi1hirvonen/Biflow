using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepReadyForProcessingListener
{
    public Task OnStepReadyForProcessingAsync(
        StepExecution stepExecution,
        OrchestratorAction stepAction,
        IStepExecutionListener orchestrationListener,
        ExtendedCancellationTokenSource cts);
}
