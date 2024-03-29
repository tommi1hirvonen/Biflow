using Biflow.Core.Constants;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public abstract class StepExecutionParameterBase : DynamicParameter, IHasExpressionParameters<StepExecutionParameterExpressionParameter, ExecutionParameter>
{
    public StepExecutionParameterBase(string parameterName, ParameterValue parameterValue, ParameterType parameterType)
    {
        ParameterName = parameterName;
        ParameterValue = parameterValue;
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
        InheritFromExecutionParameterId = parameter.InheritFromJobParameterId;
        InheritFromExecutionParameter = execution.Execution.ExecutionParameters.FirstOrDefault(p => p.ParameterId == parameter.InheritFromJobParameterId);
        UseExpression = parameter.UseExpression;
        Expression = parameter.Expression;
        _expressionParameters = parameter.ExpressionParameters
            .Select(p => new StepExecutionParameterExpressionParameter(p, execution, this))
            .ToList();
    }

    private readonly List<StepExecutionParameterExpressionParameter> _expressionParameters = [];

    public Guid ExecutionId { get; private set; }

    public Guid StepId { get; private set; }

    public ParameterType ParameterType { get; private set; }

    public override ParameterValue ParameterValue
    {
        get => InheritFromExecutionParameter is not null ? ExecutionParameterValue : base.ParameterValue;
        set => base.ParameterValue = value;
    }

    public Guid? InheritFromExecutionParameterId { get; private set; }

    public ParameterValue ExecutionParameterValue { get; set; } = new();

    public ExecutionParameter? InheritFromExecutionParameter { get; set; }

    public override bool UseExpression
    {
        get => InheritFromExecutionParameterId is null && InheritFromExecutionParameter is null && _useExpression;
        set => _useExpression = value;
    }

    private bool _useExpression;

    public IEnumerable<StepExecutionParameterExpressionParameter> ExpressionParameters => _expressionParameters;

    public abstract StepExecution BaseStepExecution { get; }

    public override string DisplayValue => (InheritFromExecutionParameter, UseExpression) switch
    {
        (not null, _) => $"{ParameterValue.Value} (inherited from execution parameter {InheritFromExecutionParameter.DisplayName})",
        (_, true) => $"{ParameterValue.Value} ({Expression.Expression})",
        _ => base.DisplayValue
    };

    public override string DisplayValueType => InheritFromExecutionParameter?.DisplayValueType ?? base.DisplayValueType;

    private bool Evaluated { get; set; }

    private object? EvaluationResult { get; set; }

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
                .ToDictionary(key => key.ParameterName, value => value.InheritFromExecutionParameter.ParameterValue.Value);
            parameters[ExpressionParameterNames.ExecutionId] = ExecutionId;
            parameters[ExpressionParameterNames.JobId] = BaseStepExecution.Execution.JobId;
            parameters[ExpressionParameterNames.StepId] = StepId;
            parameters[ExpressionParameterNames.RetryAttemptIndex] = BaseStepExecution.StepExecutionAttempts.Select(e => e.RetryAttemptIndex).Max();
            var result = await Expression.EvaluateAsync(parameters);
            EvaluationResult = result;
            Evaluated = true;
            return result;
        }

        return ParameterValue.Value;
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
        _expressionParameters.Add(expressionParameter);
    }

    public bool RemoveExpressionParameter(StepExecutionParameterExpressionParameter parameter) => _expressionParameters.Remove(parameter);
}
