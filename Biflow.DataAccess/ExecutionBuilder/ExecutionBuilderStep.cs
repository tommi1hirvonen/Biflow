using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

public class ExecutionBuilderStep
{
    private readonly ExecutionBuilder _builder;
    private readonly Step _step;

    internal ExecutionBuilderStep(ExecutionBuilder builder, Step step)
    {
        _builder = builder;
        _step = step;
    }

    public Guid StepId => _step.StepId;

    public string? StepName => _step.StepName;

    public StepType StepType => _step.StepType;

    public bool IsEnabled => _step.IsEnabled;

    public int ExecutionPhase => _step.ExecutionPhase;

    public bool IncludeInExecution() => _builder.Add(_step);

    public void IncludeWithDependencies(bool onlyOnSuccess = true) =>
        _builder.AddWithDependencies(_step, onlyOnSuccess);
}
