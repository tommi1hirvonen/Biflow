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
    public Guid ExecutionId { get; private set; }

    [Display(Name = "Step id")]
    public Guid StepId { get; private set; }

    [Display(Name = "Step")]
    public string StepName { get; private set; }

    [Display(Name = "Step type")]
    public StepType StepType { get; }

    public DuplicateExecutionBehaviour DuplicateExecutionBehaviour { get; private set; }

    public int ExecutionPhase { get; private set; }

    public int RetryAttempts { get; private set; }

    public double RetryIntervalMinutes { get; private set; }

    public EvaluationExpression ExecutionConditionExpression { get; private set; } = new();

    public Execution Execution { get; set; } = null!;

    public Step? Step { get; set; }

    public ICollection<StepExecutionAttempt> StepExecutionAttempts { get; set; } = null!;

    public ICollection<ExecutionDependency> ExecutionDependencies { get; set; } = null!;

    public ICollection<ExecutionDependency> DependantExecutions { get; set; } = null!;

    public IList<StepExecutionConditionParameter> ExecutionConditionParameters { get; set; } = null!;

    public IList<ExecutionDataObject> Sources { get; set; } = null!;

    public IList<ExecutionDataObject> Targets { get; set; } = null!;

    public override string ToString() =>
        $"{GetType().Name} {{ ExecutionId = \"{ExecutionId}\", StepId = \"{StepId}\", StepName = \"{StepName}\" }}";

    public StepExecutionStatus? ExecutionStatus => StepExecutionAttempts
            ?.MaxBy(e => e.RetryAttemptIndex)
            ?.ExecutionStatus;

    public async Task<bool> EvaluateExecutionConditionAsync()
    {
        var parameters = ExecutionConditionParameters.ToDictionary(key => key.ParameterName, value => value.ParameterValue);
        var result = await ExecutionConditionExpression.EvaluateBooleanAsync(parameters);
        return result;
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
