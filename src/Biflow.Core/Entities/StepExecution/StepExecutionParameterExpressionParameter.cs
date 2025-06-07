using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class StepExecutionParameterExpressionParameter : IExpressionParameter<ExecutionParameter>
{
    public StepExecutionParameterExpressionParameter() { }

    public StepExecutionParameterExpressionParameter(StepParameterExpressionParameter parameter, StepExecution execution, StepExecutionParameterBase executionParameter)
    {
        ExecutionId = execution.ExecutionId;
        StepParameterId = parameter.StepParameterId;
        ParameterId = parameter.ParameterId;
        ParameterName = parameter.ParameterName;
        StepParameter = executionParameter;
        InheritFromExecutionParameterId = parameter.InheritFromJobParameterId;
        InheritFromExecutionParameter = execution.Execution.ExecutionParameters.First(p => p.ParameterId == parameter.InheritFromJobParameterId);
    }

    public Guid ExecutionId { get; init; }

    public Guid StepParameterId { get; init; }

    [JsonIgnore]
    public StepExecutionParameterBase StepParameter { get; init; } = null!;

    public Guid ParameterId { get; init; }

    [Required]
    [MaxLength(128)]
    public string ParameterName { get; set; } = "";

    public Guid InheritFromExecutionParameterId { get; set; }

    [JsonIgnore]
    public ExecutionParameter InheritFromExecutionParameter { get; set; } = null!;

    [JsonIgnore]
    public ExecutionParameter InheritFromJobParameter
    {
        get => InheritFromExecutionParameter;
        set => InheritFromExecutionParameter = value;
    }

    public Guid InheritFromJobParameterId
    {
        get => InheritFromExecutionParameterId;
        set => InheritFromExecutionParameterId = value;
    }
}
