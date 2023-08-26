namespace Biflow.DataAccess.Models;

public class PipelineStepExecutionParameter : StepExecutionParameterBase
{
    public PipelineStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Pipeline, parameterValueType)
    {

    }

    public PipelineStepExecutionParameter(PipelineStepParameter parameter, PipelineStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public PipelineStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
