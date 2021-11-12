namespace EtlManager.DataAccess.Models;

public record JobStepExecutionAttempt : StepExecutionAttempt
{
    public JobStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Job)
    {
    }
}
