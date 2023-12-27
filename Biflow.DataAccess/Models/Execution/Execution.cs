using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Execution")]
public class Execution
{
    public Execution(string jobName, DateTimeOffset createdOn, ExecutionStatus executionStatus)
    {
        _jobName = jobName;
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
        _jobName = job.JobName;
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

    [Key]
    public Guid ExecutionId { get; private set; }

    [Display(Name = "Job id")]
    public Guid JobId { get; private set; }

    [Display(Name = "Job")]
    [MaxLength(250)]
    public string JobName
    {
        get => Job?.JobName ?? _jobName;
        private set
        {
            _jobName = value;
        }
    }

    private string _jobName;

    [Display(Name = "Created")]
    [DataType(DataType.DateTime)]
    public DateTimeOffset CreatedOn { get; private set; }

    [Display(Name = "Started")]
    [DataType(DataType.DateTime)]
    public DateTimeOffset? StartedOn { get; set; }

    [Display(Name = "Ended")]
    [DataType(DataType.DateTime)]
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
    [Unicode(false)]
    public string? CronExpression { get; private set; }

    [Display(Name = "Executor PID")]
    public int? ExecutorProcessId { get; set; }

    [Display(Name = "Notify")]
    public bool Notify { get; internal set; }

    [Display(Name = "Notify caller")]
    public AlertType? NotifyCaller { get; internal set; }

    [Display(Name = "Notify caller overtime")]
    public bool NotifyCallerOvertime { get; internal set; }

    [Unicode(false)]
    [MaxLength(200)]
    public StepExecutionAttemptReference? ParentExecution { get; private set; }

    public ICollection<StepExecution> StepExecutions { get; internal set; } = null!;

    public ICollection<ExecutionParameter> ExecutionParameters { get; internal set; } = null!;

    public ICollection<ExecutionConcurrency> ExecutionConcurrencies { get; internal set; } = null!;

    public ICollection<ExecutionDataObject> DataObjects { get; private set; } = null!;

    public Job? Job { get; private set; }

    public Schedule? Schedule { get; private set; }

    [NotMapped]
    public double? ExecutionInSeconds => ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;
}


public readonly record struct StepExecutionAttemptReference(Guid ExecutionId, Guid StepId, int RetryAttemptIndex);