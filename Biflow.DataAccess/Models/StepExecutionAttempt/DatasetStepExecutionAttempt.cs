namespace Biflow.DataAccess.Models;

public record DatasetStepExecutionAttempt : StepExecutionAttempt
{
    public DatasetStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Dataset)
    {
    }

    protected override void ResetInstanceMembers()
    {
        
    }
}
