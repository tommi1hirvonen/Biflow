using Biflow.Core.Entities;
using Biflow.Core.Interfaces;

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

    public bool IsEnabled => _step.IsEnabled;

    public int ExecutionPhase => _step.ExecutionPhase;

    public bool HasDependencies => _step.Dependencies.Any();

    public IEnumerable<ITag> Tags { get; }

    public bool AddToExecution() => _builder.Add(_step);

    public void AddWithDependencies(bool onlyOnSuccess = true) =>
        _builder.AddWithDependencies(_step, onlyOnSuccess);
}
