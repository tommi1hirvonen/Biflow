using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStepParameter")]
[PrimaryKey("ExecutionId", "ParameterId")]
public abstract class StepExecutionParameterBase : DynamicParameter
{
    public StepExecutionParameterBase(string parameterName, object parameterValue, ParameterType parameterType, ParameterValueType parameterValueType)
    {
        ParameterName = parameterName;
        _parameterValue = parameterValue;
        ParameterValueType = parameterValueType;
        ParameterType = parameterType;
    }

    public Guid ExecutionId { get; set; }

    public Guid StepId { get; set; }

    public ParameterType ParameterType { get; set; }

    [Column(TypeName = "sql_variant")]
    public override object? ParameterValue
    {
        get => InheritFromExecutionParameter is not null ? ExecutionParameterValue : _parameterValue;
        set => _parameterValue = value;
    }

    private object? _parameterValue;

    public override ParameterValueType ParameterValueType
    {
        get => InheritFromExecutionParameter?.ParameterValueType ?? _parameterValueType;
        set => _parameterValueType = value;
    }

    private ParameterValueType _parameterValueType = ParameterValueType.String;

    public Guid? InheritFromExecutionParameterId { get; set; }

    [Column(TypeName = "sql_variant")]
    public object? ExecutionParameterValue { get; set; }

    public ExecutionParameter? InheritFromExecutionParameter { get; set; }

    public IList<StepExecutionParameterExpressionParameter> ExpressionParameters { get; set; } = null!;

    public abstract StepExecution BaseStepExecution { get; }

    public override string DisplayValue => (InheritFromExecutionParameter, UseExpression) switch
    {
        (not null, _) => $"{InheritFromExecutionParameter.DisplayValue?.ToString() ?? "null"} (inherited from execution parameter {InheritFromExecutionParameter.DisplayName})",
        (_, true) => $"{ParameterValue} ({Expression.Expression})",
        _ => base.DisplayValue
    };

    public override string DisplayValueType => InheritFromExecutionParameter?.DisplayValueType ?? base.DisplayValueType;

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
            var parameters = ExpressionParameters
                .ToDictionary(key => key.ParameterName, value => value.InheritFromExecutionParameter.ParameterValue);
            parameters["_execution_id_"] = ExecutionId;
            parameters["_job_id_"] = BaseStepExecution.Execution.JobId;
            parameters["_step_id_"] = StepId;
            parameters["_retry_attempt_index_"] = BaseStepExecution.StepExecutionAttempts.Select(e => e.RetryAttemptIndex).Max();
            var result = await Expression.EvaluateAsync(parameters);
            EvaluationResult = result;
            Evaluated = true;
            return result;
        }

        return ParameterValue;
    }
}
