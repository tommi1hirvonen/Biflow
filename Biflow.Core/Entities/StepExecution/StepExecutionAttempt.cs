using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(AgentJobStepExecutionAttempt), nameof(StepType.AgentJob))]
[JsonDerivedType(typeof(DatasetStepExecutionAttempt), nameof(StepType.Dataset))]
[JsonDerivedType(typeof(EmailStepExecutionAttempt), nameof(StepType.Email))]
[JsonDerivedType(typeof(ExeStepExecutionAttempt), nameof(StepType.Exe))]
[JsonDerivedType(typeof(FunctionStepExecutionAttempt), nameof(StepType.Function))]
[JsonDerivedType(typeof(JobStepExecutionAttempt), nameof(StepType.Job))]
[JsonDerivedType(typeof(PackageStepExecutionAttempt), nameof(StepType.Package))]
[JsonDerivedType(typeof(PipelineStepExecutionAttempt), nameof(StepType.Pipeline))]
[JsonDerivedType(typeof(QlikStepExecutionAttempt), nameof(StepType.Qlik))]
[JsonDerivedType(typeof(SqlStepExecutionAttempt), nameof(StepType.Sql))]
[JsonDerivedType(typeof(TabularStepExecutionAttempt), nameof(StepType.Tabular))]
[JsonDerivedType(typeof(DatabricksStepExecutionAttempt), nameof(StepType.Databricks))]
[JsonDerivedType(typeof(DbtStepExecutionAttempt), nameof(StepType.Dbt))]
[JsonDerivedType(typeof(ScdStepExecutionAttempt), nameof(StepType.Scd))]
[JsonDerivedType(typeof(DataflowStepExecutionAttempt), nameof(StepType.Dataflow))]
[JsonDerivedType(typeof(FabricStepExecutionAttempt), nameof(StepType.Fabric))]
public abstract class StepExecutionAttempt(StepExecutionStatus executionStatus, StepType stepType)
{
    protected StepExecutionAttempt(StepExecutionAttempt other, int retryAttemptIndex)
        : this(other.ExecutionStatus, other.StepType)
    {
        ExecutionId = other.ExecutionId;
        StepId = other.StepId;
        RetryAttemptIndex = retryAttemptIndex;
        StepExecution = other.StepExecution;
    }

    protected StepExecutionAttempt(StepExecution execution)
        : this(StepExecutionStatus.NotStarted, execution.StepType)
    {
        ExecutionId = execution.ExecutionId;
        StepId = execution.StepId;
    }

    public Guid ExecutionId { get; [UsedImplicitly] private set; }

    public Guid StepId { get; [UsedImplicitly] private set; }

    public int RetryAttemptIndex { get; [UsedImplicitly] private set; }

    public DateTimeOffset? StartedOn { get; set; }

    public DateTimeOffset? EndedOn { get; set; }

    public StepExecutionStatus ExecutionStatus { get; set; } = executionStatus;

    public StepType StepType { get; } = stepType;

    public IList<ErrorMessage> ErrorMessages { get; [UsedImplicitly] private set; } = new List<ErrorMessage>();

    public IList<WarningMessage> WarningMessages { get; [UsedImplicitly] private set; } = new List<WarningMessage>();

    public IList<InfoMessage> InfoMessages { get; [UsedImplicitly] private set; } = new List<InfoMessage>();

    [MaxLength(250)]
    public string? StoppedBy { get; set; }

    [JsonIgnore]
    public StepExecution StepExecution { get; init; } = null!;

    [JsonIgnore]
    public string UniqueId => string.Concat(ExecutionId, StepId, RetryAttemptIndex);

    public double? ExecutionInSeconds => ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;

    [JsonIgnore]
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
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
        var error = new ErrorMessage(message, null);
        ErrorMessages.Add(error);
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
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
        var warning = new WarningMessage(message, null);
        WarningMessages.Add(warning);
    }

    public void AddOutput(string? message, bool insertFirst = false)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
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
