namespace Biflow.Core.Entities;

public class HttpStepExecutionAttempt : StepExecutionAttempt
{
    public HttpStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Http)
    {
    }

    public HttpStepExecutionAttempt(HttpStepExecutionAttempt other, int retryAttemptIndex)
        : base(other, retryAttemptIndex)
    {
    }

    public HttpStepExecutionAttempt(HttpStepExecution execution) : base(execution)
    {
    }
}