namespace Biflow.DataAccess.Models;

public class ExeStepExecutionAttempt : StepExecutionAttempt
{
    public ExeStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Exe)
    {
    }

    public ExeStepExecutionAttempt(ExeStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public ExeStepExecutionAttempt(ExeStepExecution execution) : base(execution) { }

    public int? ExeProcessId { get; set; }
}
