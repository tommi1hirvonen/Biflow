using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStep")]
[PrimaryKey("ExecutionId", "StepId")]
public abstract class StepExecution
{
    protected StepExecution(string stepName, StepType stepType)
    {
        StepName = stepName;
        StepType = stepType;
    }

    protected StepExecution(Step step, Execution execution)
    {
        ExecutionId = execution.ExecutionId;
        Execution = execution;
        StepId = step.StepId;
        StepName = step.StepName ?? "";
        StepType = step.StepType;
        DuplicateExecutionBehaviour = step.DuplicateExecutionBehaviour;
        ExecutionPhase = step.ExecutionPhase;
        RetryAttempts = step.RetryAttempts;
        RetryIntervalMinutes = step.RetryIntervalMinutes;
        ExecutionConditionExpression = step.ExecutionConditionExpression;
        ExecutionConditionParameters = step.ExecutionConditionParameters
            .Select(p => new StepExecutionConditionParameter(p, this))
            .ToArray();
        ExecutionDependencies = step.Dependencies
            .Select(d => new ExecutionDependency(d, this))
            .ToList();
        DataObjects = step.DataObjects
            .Select(d =>
            {
                var existing = execution.StepExecutions
                    .SelectMany(e => e.DataObjects.Select(x => x.DataObject))
                    .FirstOrDefault(o => o.ObjectId == d.ObjectId);
                var dataObject = existing ?? new ExecutionDataObject(d.DataObject, execution);
                return new StepExecutionDataObject(this, dataObject, d.ReferenceType, d.DataAttributes);
            })
            .ToArray();
    }

    [Display(Name = "Execution id")]
    public Guid ExecutionId { get; private set; }

    [Display(Name = "Step id")]
    public Guid StepId { get; private set; }

    [Display(Name = "Step")]
    [MaxLength(250)]
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

    public IList<StepExecutionDataObject> DataObjects { get; set; } = null!;

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
