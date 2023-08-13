namespace Biflow.DataAccess.Models;

public class ExeStepExecutionAttempt : StepExecutionAttempt
{
    public ExeStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Exe)
    {
    }

    protected ExeStepExecutionAttempt(ExeStepExecutionAttempt other) : base(other)
    {
    }

    public int? ExeProcessId { get; set; }

    protected override StepExecutionAttempt Clone() => new ExeStepExecutionAttempt(this);
}
