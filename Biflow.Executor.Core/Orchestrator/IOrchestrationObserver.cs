using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IOrchestrationObserver
{
    public void Subscribe(IOrchestrationObservable observable);

    public void OnStepExecutionStatusChange(StepExecutionStatusInfo statusChange);

    public Task WaitForOrchestrationAsync(Func<StepExecution, StepAction, Task> onReadyForOrchestration, CancellationToken cancellationToken);
}
