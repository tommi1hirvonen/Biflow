using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepProcessingListener
{
    public Task OnPreQueuedAsync(IStepProcessingContext context, StepAction stepAction);

    public Task OnPreExecuteAsync(IStepProcessingContext context, ExtendedCancellationTokenSource cts);

    public Task OnPostExecuteAsync(IStepProcessingContext context);
}
