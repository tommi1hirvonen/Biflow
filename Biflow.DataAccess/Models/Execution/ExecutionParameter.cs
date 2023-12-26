using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionParameter")]
[PrimaryKey("ExecutionId", "ParameterId")]
public class ExecutionParameter : DynamicParameter
{
    public ExecutionParameter(string parameterName, object? parameterValue, ParameterValueType parameterValueType)
    {
        ParameterName = parameterName;
        ParameterValue = parameterValue;
        DefaultValue = parameterValue;
        ParameterValueType = parameterValueType;
    }

    public ExecutionParameter(JobParameter parameter, Execution execution)
        : this(parameter.ParameterName, parameter.ParameterValue, parameter.ParameterValueType)
    {
        ExecutionId = execution.ExecutionId;
        Execution = execution;
        UseExpression = parameter.UseExpression;
        Expression = parameter.Expression;
        ParameterId = parameter.ParameterId;
    }

    public Guid ExecutionId { get; private set; }

    [Column(TypeName = "sql_variant")]
    public object? DefaultValue { get; private set; }

    public Execution Execution { get; set; } = null!;

    public ICollection<StepExecutionParameterBase> StepExecutionParameters { get; set; } = null!;

    public IList<StepExecutionParameterExpressionParameter> StepExecutionParameterExpressionParameters { get; set; } = null!;

    public ICollection<StepExecutionConditionParameter> ExecutionConditionParameters { get; set; } = null!;

    public ICollection<SqlStepExecution> CapturingStepExecutions { get; set; } = null!;

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
            var parameters = new Dictionary<string, object?>
            {
                { ExpressionParameterNames.ExecutionId, ExecutionId },
                { ExpressionParameterNames.JobId, Execution.JobId }
            };
            var result = await Expression.EvaluateAsync(parameters);
            EvaluationResult = result;
            Evaluated = true;
            return result;
        }

        return ParameterValue;
    }
}
