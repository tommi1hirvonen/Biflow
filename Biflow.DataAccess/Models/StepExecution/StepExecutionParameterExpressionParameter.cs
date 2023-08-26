using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStepParameterExpressionParameter")]
[PrimaryKey("ExecutionId", "StepParameterId", "ParameterName")]
public class StepExecutionParameterExpressionParameter
{
    public StepExecutionParameterExpressionParameter() { }

    public StepExecutionParameterExpressionParameter(StepParameterExpressionParameter parameter, StepExecutionParameterBase execution)
    {
        ExecutionId = execution.ExecutionId;
        StepParameterId = parameter.StepParameterId;
        ParameterName = parameter.ParameterName;
        StepParameter = execution;
        InheritFromExecutionParameterId = parameter.InheritFromJobParameterId;
        InheritFromExecutionParameter = execution.BaseStepExecution.Execution.ExecutionParameters.First(p => p.ParameterId == parameter.InheritFromJobParameterId);
    }

    public Guid ExecutionId { get; set; }

    public Guid StepParameterId { get; set; }

    public StepExecutionParameterBase StepParameter { get; set; } = null!;

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string ParameterName { get; set; } = "";

    public Guid InheritFromExecutionParameterId { get; set; }

    public ExecutionParameter InheritFromExecutionParameter { get; set; } = null!;
}
