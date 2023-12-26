using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStepAttempt")]
[PrimaryKey("ExecutionId", "StepId", "RetryAttemptIndex")]
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

    public DateTimeOffset? StartDateTime { get; set; }

    public DateTimeOffset? EndDateTime { get; set; }

    [MaxLength(50)]
    [Unicode(false)]
    public StepExecutionStatus ExecutionStatus { get; set; }

    [MaxLength(20)]
    [Unicode(false)]
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

    [NotMapped]
    public string UniqueId => string.Concat(ExecutionId, StepId, RetryAttemptIndex);

    [NotMapped]
    public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;

    [NotMapped]
    public bool CanBeStopped =>
        ExecutionStatus == StepExecutionStatus.Running
        || ExecutionStatus == StepExecutionStatus.AwaitingRetry
        || ExecutionStatus == StepExecutionStatus.Queued
        || ExecutionStatus == StepExecutionStatus.NotStarted && StepExecution.Execution.ExecutionStatus == Models.ExecutionStatus.Running;
}
