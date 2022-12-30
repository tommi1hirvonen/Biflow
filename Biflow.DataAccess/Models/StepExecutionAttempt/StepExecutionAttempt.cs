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

    public Guid ExecutionId { get; set; }

    public Guid StepId { get; set; }

    public int RetryAttemptIndex { get; set; }

    public DateTimeOffset? StartDateTime { get; set; }

    public DateTimeOffset? EndDateTime { get; set; }

    public StepExecutionStatus ExecutionStatus { get; set; }

    public StepType StepType { get; }

    [Display(Name = "Error message")]
    public string? ErrorMessage { get; set; }

    public string? ErrorStackTrace { get; set; }

    [Display(Name = "Warning message")]
    public string? WarningMessage { get; set; }

    [Display(Name = "Info message")]
    public string? InfoMessage { get; set; }

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
    public abstract StepExecutionAttempt Clone();
}
