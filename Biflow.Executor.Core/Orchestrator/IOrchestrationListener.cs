using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IOrchestrationListener
{
    public Task OnPreQueuedAsync(IStepOrchestrationContext context, StepAction stepAction);

    public Task OnPreExecuteAsync(IStepOrchestrationContext context, ExtendedCancellationTokenSource cts);

    public Task OnPostExecuteAsync(IStepOrchestrationContext context);
}
