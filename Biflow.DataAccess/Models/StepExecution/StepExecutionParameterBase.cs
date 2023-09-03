using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStepParameter")]
[PrimaryKey("ExecutionId", "ParameterId")]
public abstract class StepExecutionParameterBase : DynamicParameter, IHasExpressionParameters<StepExecutionParameterExpressionParameter, ExecutionParameter>
{
    public StepExecutionParameterBase(string parameterName, object parameterValue, ParameterType parameterType, ParameterValueType parameterValueType)
    {
        ParameterName = parameterName;
        ParameterValue = parameterValue;
        ParameterValueType = parameterValueType;
        ParameterType = parameterType;
    }

    public StepExecutionParameterBase(StepParameterBase parameter, StepExecution execution)
    {
        ExecutionId = execution.ExecutionId;
        StepId = parameter.StepId;
        ParameterId = parameter.ParameterId;
        ParameterType = parameter.ParameterType;
        ParameterName = parameter.ParameterName;
        ParameterValue = parameter.ParameterValue;
        ParameterValueType = parameter.ParameterValueType;
        InheritFromExecutionParameterId = parameter.InheritFromJobParameterId;
        InheritFromExecutionParameter = execution.Execution.ExecutionParameters.FirstOrDefault(p => p.ParameterId == parameter.InheritFromJobParameterId);
        UseExpression = parameter.UseExpression;
        Expression = parameter.Expression;
        ExpressionParameters = parameter.ExpressionParameters
            .Select(p => new StepExecutionParameterExpressionParameter(p, execution, this))
            .ToArray();
    }

    public Guid ExecutionId { get; private set; }

    public Guid StepId { get; private set; }

    public ParameterType ParameterType { get; private set; }

    [Column(TypeName = "sql_variant")]
    public override object? ParameterValue
    {
        get => InheritFromExecutionParameter is not null ? ExecutionParameterValue : base.ParameterValue;
        set => base.ParameterValue = value;
    }

    public override ParameterValueType ParameterValueType
    {
        get => InheritFromExecutionParameter?.ParameterValueType ?? _parameterValueType;
        set => _parameterValueType = value;
    }

    private ParameterValueType _parameterValueType = ParameterValueType.String;

    public Guid? InheritFromExecutionParameterId { get; private set; }

    [Column(TypeName = "sql_variant")]
    public object? ExecutionParameterValue { get; set; }

    public ExecutionParameter? InheritFromExecutionParameter { get; set; }

    public IList<StepExecutionParameterExpressionParameter> ExpressionParameters { get; set; } = null!;

    public abstract StepExecution BaseStepExecution { get; }

    public override string DisplayValue => (InheritFromExecutionParameter, UseExpression) switch
    {
        (not null, _) => $"{InheritFromExecutionParameter.DisplayValue} (inherited from execution parameter {InheritFromExecutionParameter.DisplayName})",
        (_, true) => $"{ParameterValue} ({Expression.Expression})",
        _ => base.DisplayValue
    };

    public override string DisplayValueType => InheritFromExecutionParameter?.DisplayValueType ?? base.DisplayValueType;

    [NotMapped]
    private bool Evaluated { get; set; }

    [NotMapped]
    private object? EvaluationResult { get; set; }

    [NotMapped]
    public IEnumerable<ExecutionParameter> JobParameters => BaseStepExecution.Execution.ExecutionParameters;

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
            parameters[ExpressionParameterNames.ExecutionId] = ExecutionId;
            parameters[ExpressionParameterNames.JobId] = BaseStepExecution.Execution.JobId;
            parameters[ExpressionParameterNames.StepId] = StepId;
            parameters[ExpressionParameterNames.RetryAttemptIndex] = BaseStepExecution.StepExecutionAttempts.Select(e => e.RetryAttemptIndex).Max();
            var result = await Expression.EvaluateAsync(parameters);
            EvaluationResult = result;
            Evaluated = true;
            return result;
        }

        return ParameterValue;
    }

    public void AddExpressionParameter(ExecutionParameter jobParameter)
    {
        var expressionParameter = new StepExecutionParameterExpressionParameter
        {
            ExecutionId = ExecutionId,
            StepParameter = this,
            StepParameterId = ParameterId,
            InheritFromExecutionParameter = jobParameter,
            InheritFromExecutionParameterId = jobParameter.ParameterId
        };
        ExpressionParameters.Add(expressionParameter);
    }
}
