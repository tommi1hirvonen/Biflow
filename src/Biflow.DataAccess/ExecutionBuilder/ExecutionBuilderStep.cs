namespace Biflow.DataAccess;

public class ExecutionBuilderStep
{
    private readonly ExecutionBuilder _builder;
    private readonly Step _step;

    internal ExecutionBuilderStep(ExecutionBuilder builder, Step step)
    {
        _builder = builder;
        _step = step;
        Tags = _step.Tags.Select(t => new ExecutionBuilderTag(t)).ToArray();
    }

    public Guid StepId => _step.StepId;

    public string? StepName => _step.StepName;

    public StepType StepType => _step.StepType;
    
    public DisplayStepType DisplayStepType => _step.DisplayStepType;

    public bool IsEnabled => _step.IsEnabled;

    public int ExecutionPhase => _step.ExecutionPhase;

    public bool HasDependencies => _step.Dependencies.Count != 0;

    public IEnumerable<ITag> Tags { get; }

    public IReadOnlyCollection<Step> AddToExecution(bool autoIncludeJobParameterDependencies) =>
        _builder.Add(_step, autoIncludeJobParameterDependencies);

    public void AddWithDependencies(bool onlyOnSuccess) => _builder.AddWithDependencies(_step, onlyOnSuccess);
}
