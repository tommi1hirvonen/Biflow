using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public abstract class StepExecution(string stepName, StepType stepType)
{
    protected StepExecution(Step step, Execution execution) : this(step.StepName ?? "", step.StepType)
    {
        ExecutionId = execution.ExecutionId;
        Execution = execution;
        StepId = step.StepId;
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

    private readonly List<StepExecutionAttempt> _stepExecutionAttempts = [];

    [Display(Name = "Execution id")]
    public Guid ExecutionId { get; [UsedImplicitly] private set; }

    [Display(Name = "Step id")]
    public Guid StepId { get; [UsedImplicitly] private set; }

    [Display(Name = "Step")]
    [MaxLength(250)]
    public string StepName { get; [UsedImplicitly] private set; } = stepName;

    [Display(Name = "Step type")]
    public StepType StepType { get; } = stepType;

    public DuplicateExecutionBehaviour DuplicateExecutionBehaviour { get; private set; }

    public int ExecutionPhase { get; private set; }

    public int RetryAttempts { get; private set; }

    public double RetryIntervalMinutes { get; private set; }

    public EvaluationExpression ExecutionConditionExpression { get; [UsedImplicitly] private set; } = new();

    [JsonIgnore]
    public Execution Execution { get; init; } = null!;

    public IEnumerable<StepExecutionAttempt> StepExecutionAttempts => _stepExecutionAttempts;

    public ICollection<ExecutionDependency> ExecutionDependencies { get; } = new List<ExecutionDependency>();

    public IEnumerable<StepExecutionConditionParameter> ExecutionConditionParameters { get; } = new List<StepExecutionConditionParameter>();

    public IEnumerable<StepExecutionDataObject> DataObjects { get; } = new List<StepExecutionDataObject>();

    public IEnumerable<StepExecutionMonitor> MonitoredStepExecutions { get; } = new List<StepExecutionMonitor>();

    public IEnumerable<StepExecutionMonitor> MonitoringStepExecutions { get; } = new List<StepExecutionMonitor>();

    public abstract StepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default);

    protected void AddAttempt(StepExecutionAttempt attempt) => _stepExecutionAttempts.Add(attempt);

    /// <summary>
    /// Get the <see cref="Step"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetStep(Step?)"/> will need to have been called first for the <see cref="Step"/> to be available.
    /// </summary>
    /// <returns><see cref="Step"/> if it was previously set using <see cref="SetStep(Step?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public Step? GetStep() => _step;

    /// <summary>
    /// Set the private <see cref="Step"/> object used for containing a possible step reference.
    /// It can be later accessed using <see cref="GetStep"/>.
    /// </summary>
    /// <param name="step"><see cref="Step"/> reference to store.
    /// The StepIds are compared and the value is set only if the ids match.</param>
    public void SetStep(Step? step)
    {
        if (step?.StepId == StepId)
        {
            _step = step;
        }
    }

    // Use a field excluded from the EF model to store the Step reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private Step? _step;

    public override string ToString() =>
        $"{GetType().Name} {{ ExecutionId = \"{ExecutionId}\", StepId = \"{StepId}\", StepName = \"{StepName}\" }}";

    public StepExecutionStatus? ExecutionStatus => StepExecutionAttempts
            .MaxBy(e => e.RetryAttemptIndex)
            ?.ExecutionStatus;

    public async Task<bool> EvaluateExecutionConditionAsync()
    {
        var parameters = ExecutionConditionParameters.ToDictionary(key => key.ParameterName, value => value.ParameterValue.Value);
        var result = await ExecutionConditionExpression.EvaluateBooleanAsync(parameters);
        return result;
    }

    public double GetDurationInSeconds()
    {
        var lastExecution = StepExecutionAttempts
            .OrderByDescending(e => e.StartedOn)
            .First();
        var endTime = lastExecution.EndedOn ?? DateTimeOffset.Now;

        var firstExecution = StepExecutionAttempts
            .OrderBy(e => e.StartedOn)
            .First();
        var startTime = firstExecution.StartedOn ?? DateTimeOffset.Now;

        var duration = (endTime - startTime).TotalSeconds;
        return duration;
    }

}
