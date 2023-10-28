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

    protected StepExecutionAttempt(StepExecutionAttempt other)
    {
        ExecutionId = other.ExecutionId;
        StepId = other.StepId;
        RetryAttemptIndex = other.RetryAttemptIndex;
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

    public StepExecutionStatus ExecutionStatus { get; set; }

    public StepType StepType { get; }

    [Display(Name = "Error message")]
    public IList<ErrorMessage> ErrorMessages { get; set; } = [];

    [Display(Name = "Warning message")]
    public IList<WarningMessage> WarningMessages { get; set; } = [];

    [Display(Name = "Info message")]
    public IList<InfoMessage> InfoMessages { get; set; } = [];

    [Display(Name = "Stopped by")]
    public string? StoppedBy { get; set; }

    public StepExecution StepExecution { get; set; } = null!;

    [NotMapped]
    public string UniqueId => string.Concat(ExecutionId, StepId, RetryAttemptIndex);

    [NotMapped]
    public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;

    /// <summary>
    /// Creates a new instance where the execution attempt specific properties have not yet been set.
    /// The returned new instance can act as a placeholder for a new execution attempt.
    /// </summary>
    /// <returns>New instance where non-attempt specific properties have been copied from this instance.</returns>
    public StepExecutionAttempt Clone(int retryAttemptIndex)
    {
        var clone = Clone();
        clone.RetryAttemptIndex = retryAttemptIndex;
        return clone;
    }

    protected abstract StepExecutionAttempt Clone(); 

    [NotMapped]
    public bool CanBeStopped =>
        ExecutionStatus == StepExecutionStatus.Running
        || ExecutionStatus == StepExecutionStatus.AwaitingRetry
        || ExecutionStatus == StepExecutionStatus.Queued
        || ExecutionStatus == StepExecutionStatus.NotStarted && StepExecution.Execution.ExecutionStatus == Models.ExecutionStatus.Running;

}
