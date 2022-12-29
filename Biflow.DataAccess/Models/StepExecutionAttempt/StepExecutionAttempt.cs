using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStepAttempt")]
[PrimaryKey("ExecutionId", "StepId", "RetryAttemptIndex")]
public abstract record StepExecutionAttempt
{
    public StepExecutionAttempt(StepExecutionStatus executionStatus, StepType stepType)
    {
        ExecutionStatus = executionStatus;
        StepType = stepType;
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
    /// Used when the step execution attempt is copied and its execution attempt specific variables should be reset.
    /// </summary>
    public void Reset()
    {
        StartDateTime = null;
        EndDateTime = null;
        ExecutionStatus = StepExecutionStatus.NotStarted;
        ErrorMessage = null;
        InfoMessage = null;
        ResetInstanceMembers();
    }

    /// <summary>
    /// Resets execution attempt specific variables to their default values.
    /// </summary>
    protected abstract void ResetInstanceMembers();
}
