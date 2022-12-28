using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

/// <summary>
/// All inheriting classes should mark properties that need to be reset
/// when copying the execution attempt for retry with the IncludeInReset attribute.
/// </summary>
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

    [IncludeInReset]
    public DateTimeOffset? StartDateTime { get; set; }

    [IncludeInReset]
    public DateTimeOffset? EndDateTime { get; set; }

    [IncludeInReset]
    public StepExecutionStatus ExecutionStatus { get; set; }

    public StepType StepType { get; }

    [Display(Name = "Error message")]
    [IncludeInReset]
    public string? ErrorMessage { get; set; }

    [Display(Name = "Info message")]
    [IncludeInReset]
    public string? InfoMessage { get; set; }

    [Display(Name = "Stopped by")]
    public string? StoppedBy { get; set; }

    public StepExecution StepExecution { get; set; } = null!;

    [NotMapped]
    public string UniqueId => string.Concat(ExecutionId, StepId, RetryAttemptIndex);

    [NotMapped]
    public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;

    /// <summary>
    /// Iterates through all properties marked with the [IncludeInReset] attribute and sets them to their default value.
    /// </summary>
    public StepExecutionAttempt Reset()
    {
        var properties = GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(IncludeInReset)));
        foreach (var prop in properties)
        {
            prop.SetValue(this, null);
        }
        return this;
    }
}
