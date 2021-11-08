using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtlManagerDataAccess.Models;

public class Execution
{

    public Execution(string jobName, DateTimeOffset createdDateTime, ExecutionStatus executionStatus)
    {
        JobName = jobName;
        CreatedDateTime = createdDateTime;
        ExecutionStatus = executionStatus;
    }

    [Key]
    public Guid ExecutionId { get; set; }

    [Display(Name = "Job id")]
    public Guid? JobId { get; set; }

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
    public bool DependencyMode { get; set; }

    [Display(Name = "Stop on first error")]
    public bool StopOnFirstError { get; set; }

    [Display(Name = "Max parallel steps (0 = use default)")]
    public int MaxParallelSteps { get; set; }

    [Display(Name = "Notification time limit (min, 0 = indefinite)")]
    public int OvertimeNotificationLimitMinutes { get; set; }

    [Display(Name = "Created by")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Schedule id")]
    public Guid? ScheduleId { get; set; }

    [Display(Name = "Executor PID")]
    public int? ExecutorProcessId { get; set; }

    public ICollection<StepExecution> StepExecutions { get; set; } = null!;

    public ICollection<ExecutionParameter> ExecutionParameters { get; set; } = null!;

    public Job? Job { get; set; }

    [NotMapped]
    public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;
}
