using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;
internal interface IStepOrchestrationContext
{
    public void ShouldFailWithStatus(StepExecutionStatus value);
}
