using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;
internal interface IStepProcessingContext
{
    public void ShouldFailWithStatus(StepExecutionStatus value, string? errorMessage = null);
}
