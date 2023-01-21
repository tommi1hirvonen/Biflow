using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionParameter")]
[PrimaryKey("ExecutionId", "ParameterId")]
public class ExecutionParameter : ParameterBase
{
    public ExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
    {
        ParameterName = parameterName;
        ParameterValue = parameterValue;
        ParameterValueType = parameterValueType;
    }

    public Guid ExecutionId { get; set; }

    public Execution Execution { get; set; } = null!;

    public ICollection<StepExecutionParameterBase> StepExecutionParameters { get; set; } = null!;

    public ICollection<StepExecutionConditionParameter> ExecutionConditionParameters { get; set; } = null!;
}
