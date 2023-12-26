using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("StepParameter")]
[PrimaryKey("ParameterId")]
public abstract class StepParameterBase : DynamicParameter, IHasExpressionParameters<StepParameterExpressionParameter, JobParameter>
{
    public StepParameterBase(ParameterType parameterType)
    {
        ParameterType = parameterType;
    }

    protected StepParameterBase(StepParameterBase other, Step step, Job? job)
    {
        ParameterId = Guid.NewGuid();
        ParameterName = other.ParameterName;
        ParameterValue = other.ParameterValue;
        ParameterValueType = other.ParameterValueType;
        UseExpression = other.UseExpression;
        Expression = new() { Expression = other.Expression.Expression };
        ParameterType = other.ParameterType;
        StepId = step.StepId;

        // The target job is set, the JobParameter is not null and the target job has a parameter with a matching name.
        if (job is not null && other.InheritFromJobParameter is not null && job.JobParameters.FirstOrDefault(p => p.ParameterName == other.InheritFromJobParameter.ParameterName) is JobParameter parameter)
        {
            InheritFromJobParameterId = parameter.ParameterId;
            InheritFromJobParameter = parameter;
        }
        // The target job has no parameter with a mathing name, so add one.
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

        ExpressionParameters = other.ExpressionParameters
            .Select(p => new StepParameterExpressionParameter(p, this, job))
            .ToList();
    }

    [Display(Name = "Step id")]
    [Column("StepId")]
    public Guid StepId { get; set; }

    [Required]
    [MaxLength(20)]
    [Unicode(false)]
    public ParameterType ParameterType { get; }

    public Guid? InheritFromJobParameterId { get; set; }

    [JsonIgnore]
    public JobParameter? InheritFromJobParameter { get; set; }

    [ValidateComplexType]
    public IList<StepParameterExpressionParameter> ExpressionParameters { get; set; } = new List<StepParameterExpressionParameter>();

    [JsonIgnore]
    public abstract Step BaseStep { get; }

    public override async Task<object?> EvaluateAsync()
    {
        if (InheritFromJobParameter is not null)
        {
            return await InheritFromJobParameter.EvaluateAsync();
        }

        if (UseExpression)
        {
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

        return ParameterValue;
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
        ExpressionParameters.Add(expressionParameter);
    }

    [JsonIgnore]
    public override string DisplayValue => InheritFromJobParameter switch
    {
        not null => $"{InheritFromJobParameter.DisplayValue?.ToString() ?? "null"} (inherited from job parameter {InheritFromJobParameter.DisplayName})",
        _ => base.DisplayValue
    };

    [JsonIgnore]
    public override string DisplayValueType => InheritFromJobParameter?.DisplayValueType ?? base.DisplayValueType;

    [NotMapped]
    [JsonIgnore]
    public IEnumerable<JobParameter> JobParameters => BaseStep.Job.JobParameters;
}
