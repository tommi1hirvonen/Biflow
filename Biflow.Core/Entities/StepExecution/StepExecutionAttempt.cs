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

    public IList<ErrorMessage> ErrorMessages { get; private set; } = new List<ErrorMessage>();

    public IList<WarningMessage> WarningMessages { get; private set; } = new List<WarningMessage>();

    public IList<InfoMessage> InfoMessages { get; private set; } = new List<InfoMessage>();

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

    public void AddError(Exception? ex, string message, bool insertFirst = false)
    {
        var error = new ErrorMessage(message, ex?.ToString());
        if (insertFirst)
        {
            ErrorMessages.Insert(0, error);
        }
        else
        {
            ErrorMessages.Add(error);
        }
    }

    public void AddError(Exception ex)
    {
        var error = new ErrorMessage(ex.Message, ex.ToString());
        ErrorMessages.Add(error);
    }

    public void AddError(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            var error = new ErrorMessage(message, null);
            ErrorMessages.Add(error);
        }
    }

    public void AddWarning(Exception? ex, string message)
    {
        var warning = new WarningMessage(message, ex?.ToString());
        WarningMessages.Add(warning);
    }

    public void AddWarning(Exception ex)
    {
        var warning = new WarningMessage(ex.Message, ex.ToString());
        WarningMessages.Add(warning);
    }

    public void AddWarning(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            var warning = new WarningMessage(message, null);
            WarningMessages.Add(warning);
        }
    }

    public void AddOutput(string? message, bool insertFirst = false)
    {
        if (!string.IsNullOrEmpty(message))
        {
            var info = new InfoMessage(message);
            if (insertFirst)
            {
                InfoMessages.Insert(0, info);
            }
            else
            {
                InfoMessages.Add(info);
            }
        }
    }
}
