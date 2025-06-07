using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class QlikStepExecutionAttempt : StepExecutionAttempt
{
    public QlikStepExecutionAttempt(StepExecutionStatus executionStatus) : base(executionStatus, StepType.Qlik)
    {
        ExecutionStatus = executionStatus;
    }

    public QlikStepExecutionAttempt(QlikStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public QlikStepExecutionAttempt(QlikStepExecution execution) : base(execution) { }

    [MaxLength(50)]
    public string? ReloadOrRunId { get; set; }
}
