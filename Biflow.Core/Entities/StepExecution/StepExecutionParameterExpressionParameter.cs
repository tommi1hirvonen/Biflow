using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

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

    public Guid ExecutionId { get; set; }

    public Guid StepParameterId { get; set; }

    public StepExecutionParameterBase StepParameter { get; set; } = null!;

    public Guid ParameterId { get; set; }

    [Required]
    [MaxLength(128)]
    public string ParameterName { get; set; } = "";

    public Guid InheritFromExecutionParameterId { get; set; }

    public ExecutionParameter InheritFromExecutionParameter { get; set; } = null!;

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
