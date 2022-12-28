namespace Biflow.DataAccess.Models;

public record EmailStepExecutionAttempt : StepExecutionAttempt
{
    public EmailStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Email)
    {
    }

    protected override void ResetInstanceMembers()
    {
        
    }
}
