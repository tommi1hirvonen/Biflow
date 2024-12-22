using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class ExeStepExecutionParameter : StepExecutionParameterBase
{
    public ExeStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Exe)
    {

    }

    public ExeStepExecutionParameter(ExeStepParameter parameter, ExeStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    [JsonIgnore]
    public ExeStepExecution StepExecution { get; init; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
