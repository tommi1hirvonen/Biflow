using Biflow.Core.Constants;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public abstract class StepParameterBase : DynamicParameter, IHasExpressionParameters<StepParameterExpressionParameter, JobParameter>
{
    protected StepParameterBase(ParameterType parameterType)
    {
        ParameterType = parameterType;
    }

    protected StepParameterBase(StepParameterBase other, Step step, Job? job)
    {
        ParameterId = Guid.NewGuid();
        ParameterName = other.ParameterName;
        ParameterValue = other.ParameterValue;
        UseExpression = other.UseExpression;
        Expression = new() { Expression = other.Expression.Expression };
        ParameterType = other.ParameterType;
        StepId = step.StepId;

        // The target job is set, the JobParameter is not null and the target job has a parameter with a matching name.
        if (job is not null
            && other.InheritFromJobParameter is not null
            && job.JobParameters.FirstOrDefault(p => p.ParameterName == other.InheritFromJobParameter.ParameterName) is { } parameter)
        {
            InheritFromJobParameterId = parameter.ParameterId;
            InheritFromJobParameter = parameter;
        }
        // The target job has no parameter with a matching name, so add one.
        else if (job is not null && other.InheritFromJobParameter is not null)
        {
            var newParameter = new JobParameter(other.InheritFromJobParameter, job);
            InheritFromJobParameter = newParameter;
            InheritFromJobParameterId = newParameter.ParameterId;
        }
        else
        {
            InheritFromJobParameterId = other.InheritFromJobParameterId;
            InheritFromJobParameter = other.InheritFromJobParameter;
        }

        _expressionParameters = other.ExpressionParameters
            .Select(p => new StepParameterExpressionParameter(p, this, job))
            .ToList();
    }

    public Guid StepId { get; init; }

    public ParameterType ParameterType { get; }

    public Guid? InheritFromJobParameterId
    {
        get;
        set
        {
            if (value is not null)
            {
                UseExpression = false;
                ParameterValue = new();
            }
            field = value;
        }
    }

    [JsonIgnore]
    public JobParameter? InheritFromJobParameter
    {
        get;
        set
        {
            if (value is not null)
            {
                UseExpression = false;
                ParameterValue = new();
            }
            field = value;
        }
    }

    [ValidateComplexType]
    [JsonIgnore]
    public IEnumerable<StepParameterExpressionParameter> ExpressionParameters => _expressionParameters;

    [JsonInclude]
    [JsonPropertyName("expressionParameters")]
#pragma warning disable IDE0044 // Skip readonly modifier so that JSON deserialization is able to assign the field.
    private List<StepParameterExpressionParameter> _expressionParameters = [];
#pragma warning restore IDE0044

    [JsonIgnore]
    public abstract Step BaseStep { get; }

    public override async Task<object?> EvaluateAsync()
    {
        if (InheritFromJobParameter is not null)
        {
            return await InheritFromJobParameter.EvaluateAsync();
        }

        if (!UseExpression)
        {
            return ParameterValue.Value;
        }
        
        var parameters = new Dictionary<string, object?>();
        foreach (var parameter in ExpressionParameters)
        {
            parameters[parameter.ParameterName] = await parameter.EvaluateAsync();
        }
        parameters[ExpressionParameterNames.ExecutionId] = Guid.Empty;
        parameters[ExpressionParameterNames.JobId] = BaseStep.JobId;
        parameters[ExpressionParameterNames.StepId] = BaseStep.StepId;
        parameters[ExpressionParameterNames.RetryAttemptIndex] = 0;
        return await Expression.EvaluateAsync(parameters);
    }

    public void AddExpressionParameter(JobParameter jobParameter)
    {
        var expressionParameter = new StepParameterExpressionParameter
        {
            StepParameter = this,
            StepParameterId = ParameterId,
            InheritFromJobParameter = jobParameter,
            InheritFromJobParameterId = jobParameter.ParameterId
        };
        _expressionParameters.Add(expressionParameter);
    }

    public void AddExpressionParameter(string parameterName, Guid jobParameterId)
    {
        var expressionParameter = new StepParameterExpressionParameter
        {
            StepParameter = this,
            StepParameterId = ParameterId,
            ParameterName = parameterName,
            InheritFromJobParameterId = jobParameterId
        };
        _expressionParameters.Add(expressionParameter);
    }

    public void RemoveExpressionParameter(StepParameterExpressionParameter parameter) => _expressionParameters.Remove(parameter);

    [JsonIgnore]
    public override string DisplayValue => InheritFromJobParameter switch
    {
        not null => $"{InheritFromJobParameter.DisplayValue} (inherited from job parameter {InheritFromJobParameter.DisplayName})",
        _ => base.DisplayValue
    };

    [JsonIgnore]
    public override string DisplayValueType => InheritFromJobParameter?.DisplayValueType ?? base.DisplayValueType;

    [JsonIgnore]
    public IEnumerable<JobParameter> JobParameters => BaseStep.Job.JobParameters;
}
