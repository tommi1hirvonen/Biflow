namespace Biflow.Core.Entities;

public class DbtStepExecutionAttempt : StepExecutionAttempt
{
    public DbtStepExecutionAttempt(StepExecutionStatus executionStatus) : base(executionStatus, StepType.Dbt)
    {
        ExecutionStatus = executionStatus;
    }

    public DbtStepExecutionAttempt(DbtStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public DbtStepExecutionAttempt(DbtStepExecution execution) : base(execution) { }

    public long DbtJobRunId { get; set; }
}
