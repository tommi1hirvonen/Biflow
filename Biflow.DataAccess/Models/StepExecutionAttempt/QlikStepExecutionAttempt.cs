namespace Biflow.DataAccess.Models;

public class QlikStepExecutionAttempt : StepExecutionAttempt
{
    public QlikStepExecutionAttempt(StepExecutionStatus executionStatus) : base(executionStatus, StepType.Qlik)
    {
        ExecutionStatus = executionStatus;
    }

    protected QlikStepExecutionAttempt(QlikStepExecutionAttempt other) : base(other) { }

    public QlikStepExecutionAttempt(QlikStepExecution execution) : base(execution) { }

    public string? ReloadId { get; set; }

    protected override QlikStepExecutionAttempt Clone() => new(this);
}
