using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class HttpStepExecutionParameter : StepExecutionParameterBase
{
    public HttpStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Http)
    {
    }

    public HttpStepExecutionParameter(HttpStepParameter parameter, HttpStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    [JsonIgnore]
    public HttpStepExecution StepExecution { get; init; } = null!;

    [JsonIgnore]
    public override StepExecution BaseStepExecution => StepExecution;
}