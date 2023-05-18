using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepReadyForOrchestrationListener
{
    public Task OnStepReadyForOrchestrationAsync(StepExecution stepExecution, StepAction stepAction, IOrchestrationListener orchestrationListener, ExtendedCancellationTokenSource cts);
}
