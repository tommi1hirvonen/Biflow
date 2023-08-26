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

    public JobStepExecutionAttempt(JobStepExecution execution) : base(execution) { }

    public Guid? ChildJobExecutionId { get; set; }

    protected override StepExecutionAttempt Clone() => new JobStepExecutionAttempt(this);
}
