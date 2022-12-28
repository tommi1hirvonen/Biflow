namespace Biflow.DataAccess.Models;

public record TabularStepExecutionAttempt : StepExecutionAttempt
{
    public TabularStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Tabular)
    {
    }

    protected override void ResetInstanceMembers()
    {
        
    }
}
