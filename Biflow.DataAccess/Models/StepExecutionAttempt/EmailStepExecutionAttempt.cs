namespace Biflow.DataAccess.Models;

public class EmailStepExecutionAttempt : StepExecutionAttempt
{
    public EmailStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Email)
    {
    }

    protected EmailStepExecutionAttempt(EmailStepExecutionAttempt other) : base(other)
    {
    }

    protected override StepExecutionAttempt Clone() => new EmailStepExecutionAttempt(this);
}
