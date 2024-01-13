using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public abstract class StepExecutionAttempt
{
    public StepExecutionAttempt(StepExecutionStatus executionStatus, StepType stepType)
    {
        ExecutionStatus = executionStatus;
        StepType = stepType;
    }

    protected StepExecutionAttempt(StepExecutionAttempt other, int retryAttemptIndex)
    {
        ExecutionId = other.ExecutionId;
        StepId = other.StepId;
        RetryAttemptIndex = retryAttemptIndex;
        ExecutionStatus = other.ExecutionStatus;
        StepType = other.StepType;
        StepExecution = other.StepExecution;
    }

    protected StepExecutionAttempt(StepExecution execution)
    {
        ExecutionId = execution.ExecutionId;
        StepId = execution.StepId;
        ExecutionStatus = StepExecutionStatus.NotStarted;
        StepType = execution.StepType;
    }

    public Guid ExecutionId { get; private set; }

    public Guid StepId { get; private set; }

    public int RetryAttemptIndex { get; private set; }

    public DateTimeOffset? StartedOn { get; set; }

    public DateTimeOffset? EndedOn { get; set; }

    public StepExecutionStatus ExecutionStatus { get; set; }

    public StepType StepType { get; }

    [Display(Name = "Error message")]
    public List<ErrorMessage> ErrorMessages { get; set; } = [];

    [Display(Name = "Warning message")]
    public List<WarningMessage> WarningMessages { get; set; } = [];

    [Display(Name = "Info message")]
    public List<InfoMessage> InfoMessages { get; set; } = [];

    [Display(Name = "Stopped by")]
    [MaxLength(250)]
    public string? StoppedBy { get; set; }

    public StepExecution StepExecution { get; set; } = null!;

    public string UniqueId => string.Concat(ExecutionId, StepId, RetryAttemptIndex);

    public double? ExecutionInSeconds => ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;

    public bool CanBeStopped =>
        ExecutionStatus == StepExecutionStatus.Running
        || ExecutionStatus == StepExecutionStatus.AwaitingRetry
        || ExecutionStatus == StepExecutionStatus.Queued
        || ExecutionStatus == StepExecutionStatus.NotStarted && StepExecution.Execution.ExecutionStatus == Entities.ExecutionStatus.Running;
}
