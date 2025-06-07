using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class Execution(string jobName, DateTimeOffset createdOn, ExecutionStatus executionStatus)
{
    public Execution(Job job, string? createdBy, StepExecutionAttempt? parent = null) : this(job)
    {
        CreatedBy = createdBy;
        if (parent is not null)
        {
            ParentExecution = new(parent.ExecutionId, parent.StepId, parent.RetryAttemptIndex);
        }
    }

    public Execution(Job job, Schedule? schedule) : this(job)
    {
        ScheduleId = schedule?.ScheduleId;
        ScheduleName = schedule?.ScheduleName;
        CronExpression = schedule?.CronExpression;
        Notify = true;
    }

    private Execution(Job job) : this(job.JobName, DateTimeOffset.Now, ExecutionStatus.NotStarted)
    {
        ExecutionId = Guid.NewGuid();
        JobId = job.JobId;
        ExecutionMode = job.ExecutionMode;
        StopOnFirstError = job.StopOnFirstError;
        MaxParallelSteps = job.MaxParallelSteps;
        TimeoutMinutes = job.TimeoutMinutes;
        OvertimeNotificationLimitMinutes = job.OvertimeNotificationLimitMinutes;
        StepExecutions = new List<StepExecution>();
        ExecutionConcurrencies = job.JobConcurrencies
            .Select(c => new ExecutionConcurrency(c, this))
            .ToArray();
        ExecutionParameters = job.JobParameters
            .Select(p => new ExecutionParameter(p, this))
            .ToArray();
    }

    public Guid ExecutionId { get; private set; }

    public Guid JobId { get; private set; }

    [MaxLength(250)]
    public string JobName { get; private set; } = jobName;

    public DateTimeOffset CreatedOn { get; private set; } = createdOn;

    public DateTimeOffset? StartedOn { get; set; }

    public DateTimeOffset? EndedOn { get; set; }

    public ExecutionStatus ExecutionStatus { get; set; } = executionStatus;

    public ExecutionMode ExecutionMode { get; private set; }

    public bool StopOnFirstError { get; private set; }

    public int MaxParallelSteps { get; private set; }

    public double OvertimeNotificationLimitMinutes { get; private set; }

    [MaxLength(250)]
    public string? CreatedBy { get; private set; }

    public Guid? ScheduleId { get; private set; }

    [MaxLength(250)]
    public string? ScheduleName { get; private set; }

    [MaxLength(200)]
    public string? CronExpression { get; private set; }

    public int? ExecutorProcessId { get; set; }

    public bool Notify { get; set; }

    public AlertType? NotifyCaller { get; set; }

    public bool NotifyCallerOvertime { get; set; }

    public double TimeoutMinutes { get; set; }

    [MaxLength(200)]
    public StepExecutionAttemptReference? ParentExecution { get; private set; }

    [JsonIgnore]
    public ICollection<StepExecution> StepExecutions { get; } = new List<StepExecution>();

    public IEnumerable<ExecutionParameter> ExecutionParameters { get; } = new List<ExecutionParameter>();

    public IEnumerable<ExecutionConcurrency> ExecutionConcurrencies { get; } = new List<ExecutionConcurrency>();

    public IEnumerable<ExecutionDataObject> DataObjects { get; } = new List<ExecutionDataObject>();

    public double? ExecutionInSeconds => ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;

    /// <summary>
    /// Calculates and returns the execution's status based on its child step execution attempt statuses.
    /// </summary>
    /// <returns>The execution's calculated status</returns>
    public ExecutionStatus GetCalculatedStatus()
    {
        var attempts = StepExecutions
            .SelectMany(e => e.StepExecutionAttempts)
            .ToArray();
        ExecutionStatus status;
        if (attempts.All(x => x.ExecutionStatus is StepExecutionStatus.Succeeded or StepExecutionStatus.Skipped))
        {
            status = ExecutionStatus.Succeeded;
        }
        else if (attempts.Any(x => x.ExecutionStatus is StepExecutionStatus.Failed or StepExecutionStatus.DependenciesFailed))
        {
            status = ExecutionStatus.Failed;
        }
        else if (attempts.Any(x => x.ExecutionStatus is StepExecutionStatus.Retry or StepExecutionStatus.Duplicate or StepExecutionStatus.Warning))
        {
            status = ExecutionStatus.Warning;
        }
        else if (attempts.Any(x => x.ExecutionStatus is StepExecutionStatus.Stopped))
        {
            status = ExecutionStatus.Stopped;
        }
        else if (attempts.Any(x => x.ExecutionStatus is StepExecutionStatus.NotStarted or StepExecutionStatus.Queued or StepExecutionStatus.AwaitingRetry or StepExecutionStatus.Running))
        {
            status = ExecutionStatus.Suspended;
        }
        else
        {
            status = ExecutionStatus.Failed;
        }
        return status;
    }
}