using Biflow.Core.Constants;

namespace Biflow.Core.Entities;

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

    private bool _evaluated;
    private object? _evaluationResult;

    public Guid ExecutionId { get; private set; }

    public object? DefaultValue { get; private set; }

    public Execution Execution { get; private set; } = null!;

    public IEnumerable<StepExecutionParameterBase> StepExecutionParameters { get; } = new List<StepExecutionParameterBase>();

    public IEnumerable<StepExecutionParameterExpressionParameter> StepExecutionParameterExpressionParameters { get; } = new List<StepExecutionParameterExpressionParameter>();

    public IEnumerable<StepExecutionConditionParameter> ExecutionConditionParameters { get; } = new List<StepExecutionConditionParameter>();

    public IEnumerable<SqlStepExecution> CapturingStepExecutions { get; } = new List<SqlStepExecution>();

    public override string DisplayValue =>
        UseExpression ? $"{ParameterValue} ({Expression.Expression})" : base.DisplayValue;

    public override async Task<object?> EvaluateAsync()
    {
        if (UseExpression && _evaluated)
        {
            return _evaluationResult;
        }
        else if (UseExpression)
        {
            var parameters = new Dictionary<string, object?>
            {
                { ExpressionParameterNames.ExecutionId, ExecutionId },
                { ExpressionParameterNames.JobId, Execution.JobId }
            };
            var result = await Expression.EvaluateAsync(parameters);
            _evaluationResult = result;
            _evaluated = true;
            return result;
        }

        return ParameterValue;
    }
}
