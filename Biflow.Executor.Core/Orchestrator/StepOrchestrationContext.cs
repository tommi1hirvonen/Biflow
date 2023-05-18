using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal class StepOrchestrationContext : IStepOrchestrationContext
{
    public StepExecutionStatus? FailStatus { get; private set; }

    public string? ErrorMessage { get; private set; }

    public void ShouldFailWithStatus(StepExecutionStatus value, string? errorMessage = null)
    {
        FailStatus = value;
        ErrorMessage = errorMessage;
    }
}
