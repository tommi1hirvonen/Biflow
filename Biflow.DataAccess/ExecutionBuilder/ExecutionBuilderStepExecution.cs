using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

public class ExecutionBuilderStepExecution
{
    private readonly ExecutionBuilder _builder;
    private readonly StepExecution _stepExecution;

    internal ExecutionBuilderStepExecution(ExecutionBuilder builder, StepExecution stepExecution)
    {
        _builder = builder;
        _stepExecution = stepExecution;
        SupportsParameters = stepExecution is IHasStepExecutionParameters;
        Parameters = stepExecution switch
        {
            IHasStepExecutionParameters hasParams => hasParams.StepExecutionParameters,
            _ => Enumerable.Empty<DynamicParameter>()
        };
    }

    public Guid StepId => _stepExecution.StepId;

    public string StepName => _stepExecution.StepName;

    public StepType StepType => _stepExecution.StepType;

    public int ExecutionPhase => _stepExecution.ExecutionPhase;

    public bool SupportsParameters { get; }

    public IEnumerable<DynamicParameter> Parameters { get; }

    public bool RemoveFromExecution() => _builder.Remove(_stepExecution);
}

