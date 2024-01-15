using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class Execution
{
    public Execution(string jobName, DateTimeOffset createdOn, ExecutionStatus executionStatus)
    {
        JobName = jobName;
        CreatedOn = createdOn;
        ExecutionStatus = executionStatus;
    }

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

    private Execution(Job job)
    {
        ExecutionId = Guid.NewGuid();
        JobId = job.JobId;
        JobName = job.JobName;
        CreatedOn = DateTimeOffset.Now;
        ExecutionStatus = ExecutionStatus.NotStarted;
        DependencyMode = job.UseDependencyMode;
        StopOnFirstError = job.StopOnFirstError;
        MaxParallelSteps = job.MaxParallelSteps;
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

    [Display(Name = "Job id")]
    public Guid JobId { get; private set; }

    [Display(Name = "Job")]
    [MaxLength(250)]
    public string JobName { get; private set; }

    [Display(Name = "Created")]
    public DateTimeOffset CreatedOn { get; private set; }

    [Display(Name = "Started")]
    public DateTimeOffset? StartedOn { get; set; }

    [Display(Name = "Ended")]
    public DateTimeOffset? EndedOn { get; set; }

    [Display(Name = "Status")]
    public ExecutionStatus ExecutionStatus { get; set; }

    [Display(Name = "Dependency mode")]
    public bool DependencyMode { get; private set; }

    [Display(Name = "Stop on first error")]
    public bool StopOnFirstError { get; private set; }

    [Display(Name = "Max parallel steps (0 = use default)")]
    public int MaxParallelSteps { get; private set; }

    [Display(Name = "Notification time limit (min, 0 = indefinite)")]
    public double OvertimeNotificationLimitMinutes { get; private set; }

    [Display(Name = "Created by")]
    [MaxLength(250)]
    public string? CreatedBy { get; private set; }

    [Display(Name = "Schedule id")]
    public Guid? ScheduleId { get; private set; }

    [MaxLength(250)]
    public string? ScheduleName { get; private set; }

    [MaxLength(200)]
    public string? CronExpression { get; private set; }

    [Display(Name = "Executor PID")]
    public int? ExecutorProcessId { get; set; }

    [Display(Name = "Notify")]
    public bool Notify { get; set; }

    [Display(Name = "Notify caller")]
    public AlertType? NotifyCaller { get; set; }

    [Display(Name = "Notify caller overtime")]
    public bool NotifyCallerOvertime { get; set; }

    [MaxLength(200)]
    public StepExecutionAttemptReference? ParentExecution { get; private set; }

    public ICollection<StepExecution> StepExecutions { get; } = new List<StepExecution>();

    public IEnumerable<ExecutionParameter> ExecutionParameters { get; } = new List<ExecutionParameter>();

    public IEnumerable<ExecutionConcurrency> ExecutionConcurrencies { get; } = new List<ExecutionConcurrency>();

    public IEnumerable<ExecutionDataObject> DataObjects { get; } = new List<ExecutionDataObject>();

    public double? ExecutionInSeconds => ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;
}


public readonly record struct StepExecutionAttemptReference(Guid ExecutionId, Guid StepId, int RetryAttemptIndex);