﻿using System.Text.Json.Serialization;
using Biflow.Core.Constants;

namespace Biflow.Core.Entities;

public sealed class JobParameter : DynamicParameter
{
    public JobParameter() { }

    internal JobParameter(JobParameter other, Job? job)
    {
        ParameterId = Guid.NewGuid();
        JobId = job?.JobId ?? other.JobId;
        Job = job ?? other.Job;
        ParameterName = other.ParameterName;
        ParameterValue = other.ParameterValue;
        UseExpression = other.UseExpression;
        Expression = new() { Expression = other.Expression.Expression };
        job?.JobParameters.Add(this);
    }

    public Guid JobId { get; init; }

    [JsonIgnore]
    public Job Job { get; init; } = null!;

    [JsonIgnore]
    public IEnumerable<StepParameterBase> InheritingStepParameters { get; } = new List<StepParameterBase>();

    [JsonIgnore]
    public IEnumerable<JobStepParameter> AssigningStepParameters { get; } = new List<JobStepParameter>();

    [JsonIgnore]
    public IEnumerable<StepParameterExpressionParameter> InheritingStepParameterExpressionParameters { get; } = new List<StepParameterExpressionParameter>();

    [JsonIgnore]
    public IEnumerable<SqlStep> CapturingSteps { get; } = new List<SqlStep>();

    [JsonIgnore]
    public IEnumerable<ExecutionConditionParameter> ExecutionConditionParameters { get; } = new List<ExecutionConditionParameter>();

    public override async Task<object?> EvaluateAsync()
    {
        if (!UseExpression)
        {
            return ParameterValue.Value;
        }
        var parameters = new Dictionary<string, object?> {
            { ExpressionParameterNames.ExecutionId, Guid.Empty },
            { ExpressionParameterNames.JobId, JobId }
        };
        return await Expression.EvaluateAsync(parameters);

    }
}
