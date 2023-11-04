namespace Biflow.DataAccess.Models;

public class JobStepExecutionAttempt : StepExecutionAttempt
{
    public JobStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Job)
    {
    }

    public JobStepExecutionAttempt(JobStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public JobStepExecutionAttempt(JobStepExecution execution) : base(execution) { }

    public Guid? ChildJobExecutionId { get; set; }
}
