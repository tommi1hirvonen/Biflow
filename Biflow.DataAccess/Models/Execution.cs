using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Execution")]
public class Execution
{
    public Execution(string jobName, DateTimeOffset createdDateTime, ExecutionStatus executionStatus)
    {
        JobName = jobName;
        CreatedDateTime = createdDateTime;
        ExecutionStatus = executionStatus;
    }

    public Execution(Job job, string createdBy) : this(job)
    {
        CreatedBy = createdBy;
    }

    public Execution(Job job, Schedule schedule) : this(job)
    {
        ScheduleId = schedule.ScheduleId;
        ScheduleName = schedule.ScheduleName;
        CronExpression = schedule.CronExpression;
        Notify = true;
    }

    private Execution(Job job)
    {
        ExecutionId = Guid.NewGuid();
        JobId = job.JobId;
        JobName = job.JobName;
        CreatedDateTime = DateTimeOffset.Now;
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
    public Guid? JobId { get; private set; }

    [Display(Name = "Job")]
    public string JobName { get; set; }

    [Display(Name = "Created")]
    [DataType(DataType.DateTime)]
    public DateTimeOffset CreatedDateTime { get; set; }

    [Display(Name = "Started")]
    [DataType(DataType.DateTime)]
    public DateTimeOffset? StartDateTime { get; set; }

    [Display(Name = "Ended")]
    [DataType(DataType.DateTime)]
    public DateTimeOffset? EndDateTime { get; set; }

    [Display(Name = "Status")]
    public ExecutionStatus ExecutionStatus { get; set; }

    [Display(Name = "Dependency mode")]
    public bool DependencyMode { get; private set; }

    [Display(Name = "Stop on first error")]
    public bool StopOnFirstError { get; private set; }

    [Display(Name = "Max parallel steps (0 = use default)")]
    public int MaxParallelSteps { get; private set; }

    [Display(Name = "Notification time limit (min, 0 = indefinite)")]
    public int OvertimeNotificationLimitMinutes { get; private set; }

    [Display(Name = "Created by")]
    public string? CreatedBy { get; private set; }

    [Display(Name = "Schedule id")]
    public Guid? ScheduleId { get; private set; }

    public string? ScheduleName { get; private set; }

    public string? CronExpression { get; private set; }

    [Display(Name = "Executor PID")]
    public int? ExecutorProcessId { get; set; }

    [Display(Name = "Notify")]
    public bool Notify { get; set; }

    [Display(Name = "Notify caller")]
    public SubscriptionType? NotifyCaller { get; set; }

    [Display(Name = "Notify caller overtime")]
    public bool NotifyCallerOvertime { get; set; }

    public ICollection<StepExecution> StepExecutions { get; set; } = null!;

    public ICollection<ExecutionParameter> ExecutionParameters { get; set; } = null!;

    public ICollection<ExecutionConcurrency> ExecutionConcurrencies { get; set; } = null!;

    public Job? Job { get; set; }

    public Schedule? Schedule { get; set; }

    [NotMapped]
    public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;
}
