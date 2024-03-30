namespace Biflow.Core.Entities;

public class PipelineStepExecutionParameter : StepExecutionParameterBase
{
    public PipelineStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Pipeline)
    {

    }

    public PipelineStepExecutionParameter(PipelineStepParameter parameter, PipelineStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public PipelineStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
