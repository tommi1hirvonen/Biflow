namespace Biflow.DataAccess.Models;

public record AgentJobStepExecutionAttempt : StepExecutionAttempt
{
    public AgentJobStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.AgentJob)
    {
    }

    protected override void ResetInstanceMembers()
    {
        
    }
}
