namespace Biflow.DataAccess.Models;

public record ExeStepExecutionAttempt : StepExecutionAttempt
{
    public ExeStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Exe)
    {
    }

    public int? ExeProcessId { get; set; }

    protected override void ResetInstanceMembers()
    {
        ExeProcessId = null;
    }
}
