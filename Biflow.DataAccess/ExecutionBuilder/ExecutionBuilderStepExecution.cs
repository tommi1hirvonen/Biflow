namespace Biflow.DataAccess;

public class ExecutionBuilderStepExecution
{
    private readonly StepExecution _stepExecution;

    internal ExecutionBuilderStepExecution(ExecutionBuilder builder, StepExecution stepExecution)
    {
        Builder = builder;
        _stepExecution = stepExecution;
        SupportsParameters = stepExecution is IHasStepExecutionParameters;
        Parameters = stepExecution switch
        {
            IHasStepExecutionParameters hasParams => hasParams.StepExecutionParameters,
            _ => []
        };
    }

    public ExecutionBuilder Builder { get; }

    public Guid StepId => _stepExecution.StepId;

    public string StepName => _stepExecution.StepName;

    public StepType StepType => _stepExecution.StepType;

    public int ExecutionPhase => _stepExecution.ExecutionPhase;

    public bool SupportsParameters { get; }

    public IEnumerable<StepExecutionParameterBase> Parameters { get; }

    public void RemoveFromExecution() => Builder.Remove(_stepExecution);
}

