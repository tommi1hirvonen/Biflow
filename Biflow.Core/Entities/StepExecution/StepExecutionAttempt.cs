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

    private readonly List<ErrorMessage> _errorMessages = [];
    private readonly List<WarningMessage> _warningMessages = [];
    private readonly List<InfoMessage> _infoMessages = [];

    public Guid ExecutionId { get; private set; }

    public Guid StepId { get; private set; }

    public int RetryAttemptIndex { get; private set; }

    public DateTimeOffset? StartedOn { get; set; }

    public DateTimeOffset? EndedOn { get; set; }

    public StepExecutionStatus ExecutionStatus { get; set; }

    public StepType StepType { get; }

    public IEnumerable<ErrorMessage> ErrorMessages => _errorMessages;

    public IEnumerable<WarningMessage> WarningMessages => _warningMessages;

    public IEnumerable<InfoMessage> InfoMessages => _infoMessages;    

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
            _errorMessages.Insert(0, error);
        }
        else
        {
            _errorMessages.Add(error);
        }
    }

    public void AddError(Exception ex)
    {
        var error = new ErrorMessage(ex.Message, ex.ToString());
        _errorMessages.Add(error);
    }

    public void AddError(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            var error = new ErrorMessage(message, null);
            _errorMessages.Add(error);
        }
    }

    public void AddWarning(Exception? ex, string message)
    {
        var warning = new WarningMessage(message, ex?.ToString());
        _warningMessages.Add(warning);
    }

    public void AddWarning(Exception ex)
    {
        var warning = new WarningMessage(ex.Message, ex.ToString());
        _warningMessages.Add(warning);
    }

    public void AddWarning(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            var warning = new WarningMessage(message, null);
            _warningMessages.Add(warning);
        }
    }

    public void AddOutput(string? message, bool insertFirst = false)
    {
        if (!string.IsNullOrEmpty(message))
        {
            var info = new InfoMessage(message);
            if (insertFirst)
            {
                _infoMessages.Insert(0, info);
            }
            else
            {
                _infoMessages.Add(info);
            }
        }
    }
}
