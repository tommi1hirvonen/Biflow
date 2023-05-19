using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepReadyForProcessingListener
{
    public Task OnStepReadyForProcessingAsync(StepExecution stepExecution, StepAction stepAction, IStepProcessingListener orchestrationListener, ExtendedCancellationTokenSource cts);
}
