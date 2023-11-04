namespace Biflow.DataAccess.Models;

public class EmailStepExecutionAttempt : StepExecutionAttempt
{
    public EmailStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Email)
    {
    }

    public EmailStepExecutionAttempt(EmailStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public EmailStepExecutionAttempt(EmailStepExecution execution) : base(execution) { }
}
