namespace Biflow.DataAccess.Models;

public class JobStepExecutionAttempt : StepExecutionAttempt
{
    public JobStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Job)
    {
    }

    protected JobStepExecutionAttempt(JobStepExecutionAttempt other) : base(other)
    {
    }

    public Guid? ChildJobExecutionId { get; set; }

    public override StepExecutionAttempt Clone() => new JobStepExecutionAttempt(this);
}
