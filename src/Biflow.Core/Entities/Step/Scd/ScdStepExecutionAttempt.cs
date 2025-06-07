namespace Biflow.Core.Entities;

public class ScdStepExecutionAttempt : StepExecutionAttempt
{
    public ScdStepExecutionAttempt(StepExecutionStatus executionStatus) : base(executionStatus, StepType.Scd)
    {
        ExecutionStatus = executionStatus;
    }

    public ScdStepExecutionAttempt(ScdStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public ScdStepExecutionAttempt(ScdStepExecution execution) : base(execution) { }
}
