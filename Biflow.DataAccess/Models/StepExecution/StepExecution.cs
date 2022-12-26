using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStep")]
[PrimaryKey("ExecutionId", "StepId")]
public abstract class StepExecution
{
    public StepExecution(string stepName, StepType stepType)
    {
        StepName = stepName;
        StepType = stepType;
    }

    [Display(Name = "Execution id")]
    public Guid ExecutionId { get; set; }

    [Display(Name = "Step id")]
    public Guid StepId { get; set; }

    [Display(Name = "Step")]
    public string StepName { get; set; }

    [Display(Name = "Step type")]
    public StepType StepType { get; }

    public int ExecutionPhase { get; set; }

    public int RetryAttempts { get; set; }

    public double RetryIntervalMinutes { get; set; }

    [Display(Name = "Execution condition")]
    public string? ExecutionConditionExpression { get; set; }

    public Execution Execution { get; set; } = null!;

    public Step? Step { get; set; }

    public ICollection<StepExecutionAttempt> StepExecutionAttempts { get; set; } = null!;

    public ICollection<ExecutionDependency> ExecutionDependencies { get; set; } = null!;

    public ICollection<ExecutionDependency> DependantExecutions { get; set; } = null!;

    public IList<StepExecutionConditionParameter> ExecutionConditionParameters { get; set; } = null!;

    public IList<ExecutionSourceTargetObject> Sources { get; set; } = null!;

    public IList<ExecutionSourceTargetObject> Targets { get; set; } = null!;

    public override string ToString() =>
        $"{GetType().Name} {{ ExecutionId = \"{ExecutionId}\", StepId = \"{StepId}\", StepName = \"{StepName}\" }}";

    public StepExecutionStatus GetExecutionStatus()
    {
        var lastExecution = StepExecutionAttempts
            .OrderByDescending(e => e.StartDateTime)
            .First();
        if (lastExecution.ExecutionStatus == StepExecutionStatus.AwaitRetry)
        {
            return StepExecutionStatus.Running;
        }
        else
        {
            return lastExecution.ExecutionStatus;
        }
    }

    public double GetDurationInSeconds()
    {
        var lastExecution = StepExecutionAttempts
            .OrderByDescending(e => e.StartDateTime)
            .First();
        var endTime = lastExecution.EndDateTime ?? DateTimeOffset.Now;

        var firstExecution = StepExecutionAttempts
            .OrderBy(e => e.StartDateTime)
            .First();
        var startTime = firstExecution.StartDateTime ?? DateTimeOffset.Now;

        var duration = (endTime - startTime).TotalSeconds;
        return duration;
    }

}
