namespace Biflow.Core.Entities;

public class WaitStepExecutionAttempt : StepExecutionAttempt
{
    public WaitStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Wait)
    {
    }

    public WaitStepExecutionAttempt(WaitStepExecutionAttempt other, int retryAttemptIndex)
        : base(other, retryAttemptIndex)
    {
    }

    public WaitStepExecutionAttempt(WaitStepExecution execution) : base(execution)
    {
    }
}