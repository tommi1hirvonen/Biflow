using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionParameter")]
[PrimaryKey("ExecutionId", "ParameterId")]
public class ExecutionParameter : DynamicParameter
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

    public IList<StepExecutionParameterExpressionParameter> StepExecutionParameterExpressionParameters { get; set; } = null!;

    public ICollection<StepExecutionConditionParameter> ExecutionConditionParameters { get; set; } = null!;

    public override string DisplayValue =>
        UseExpression ? $"{ParameterValue} ({Expression.Expression})" : base.DisplayValue;

    [NotMapped]
    private bool Evaluated { get; set; }

    [NotMapped]
    private object? EvaluationResult { get; set; }

    public override async Task<object?> EvaluateAsync()
    {
        if (UseExpression && Evaluated)
        {
            return EvaluationResult;
        }
        else if (UseExpression)
        {
            var result = await Expression.EvaluateAsync();
            EvaluationResult = result;
            Evaluated = true;
            return result;
        }

        return ParameterValue;
    }
}
