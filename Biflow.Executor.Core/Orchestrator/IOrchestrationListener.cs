using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IOrchestrationListener
{
    public Task OnStepReadyForOrchestration(StepExecution stepExecution, StepAction stepAction);
}
