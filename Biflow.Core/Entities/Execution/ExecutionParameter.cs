using System.Text.Json.Serialization;
using Biflow.Core.Constants;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public sealed class ExecutionParameter : DynamicParameter
{
    public ExecutionParameter(string parameterName, ParameterValue parameterValue)
    {
        ParameterName = parameterName;
        ParameterValue = parameterValue;
        DefaultValue = parameterValue;
    }

    public ExecutionParameter(JobParameter parameter, Execution execution)
        : this(parameter.ParameterName, parameter.ParameterValue)
    {
        ExecutionId = execution.ExecutionId;
        Execution = execution;
        UseExpression = parameter.UseExpression;
        Expression = parameter.Expression;
        ParameterId = parameter.ParameterId;
    }

    private bool _evaluated;
    private object? _evaluationResult;

    public Guid ExecutionId { get; [UsedImplicitly] private set; }

    public ParameterValue DefaultValue { get; private set; }

    [JsonIgnore]
    public Execution Execution { get; [UsedImplicitly] private set; } = null!;

    [JsonIgnore]
    public IEnumerable<StepExecutionParameterBase> StepExecutionParameters { get; } = new List<StepExecutionParameterBase>();

    [JsonIgnore]
    public IEnumerable<StepExecutionParameterExpressionParameter> StepExecutionParameterExpressionParameters { get; } = new List<StepExecutionParameterExpressionParameter>();

    [JsonIgnore]
    public IEnumerable<StepExecutionConditionParameter> ExecutionConditionParameters { get; } = new List<StepExecutionConditionParameter>();

    [JsonIgnore]
    public IEnumerable<SqlStepExecution> CapturingStepExecutions { get; } = new List<SqlStepExecution>();

    [JsonIgnore]
    public override string DisplayValue =>
        UseExpression ? $"{ParameterValue.Value} ({Expression.Expression})" : base.DisplayValue;

    public override async Task<object?> EvaluateAsync()
    {
        if (UseExpression && _evaluated)
        {
            return _evaluationResult;
        }

        if (!UseExpression)
        {
            return ParameterValue.Value;
        }
        
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
}
