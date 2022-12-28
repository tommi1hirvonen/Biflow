namespace Biflow.DataAccess.Models;

public record JobStepExecutionAttempt : StepExecutionAttempt
{
    public JobStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Job)
    {
    }

    public Guid? ChildJobExecutionId { get; set; }

    protected override void ResetInstanceMembers()
    {
        ChildJobExecutionId = null;
    }
}
