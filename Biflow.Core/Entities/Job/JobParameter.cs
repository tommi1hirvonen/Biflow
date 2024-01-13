using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class JobParameter : DynamicParameter
{
    public JobParameter() { }

    internal JobParameter(JobParameter other, Job? job)
    {
        ParameterId = Guid.NewGuid();
        JobId = job?.JobId ?? other.JobId;
        Job = job ?? other.Job;
        ParameterName = other.ParameterName;
        ParameterValue = other.ParameterValue;
        ParameterValueType = other.ParameterValueType;
        UseExpression = other.UseExpression;
        Expression = new() { Expression = other.Expression.Expression };
        job?.JobParameters?.Add(this);
    }

    [Display(Name = "Job")]
    public Guid JobId { get; set; }

    [JsonIgnore]
    public Job Job { get; set; } = null!;

    [JsonIgnore]
    public ICollection<StepParameterBase> InheritingStepParameters { get; set; } = null!;

    [JsonIgnore]
    public ICollection<JobStepParameter> AssigningStepParameters { get; set; } = null!;

    [JsonIgnore]
    public ICollection<StepParameterExpressionParameter> InheritingStepParameterExpressionParameters { get; set; } = null!;

    [JsonIgnore]
    public ICollection<SqlStep> CapturingSteps { get; set; } = null!;

    [JsonIgnore]
    public ICollection<ExecutionConditionParameter> ExecutionConditionParameters { get; set; } = null!;

    public override async Task<object?> EvaluateAsync()
    {
        if (UseExpression)
        {
            var parameters = new Dictionary<string, object?> {
                { ExpressionParameterNames.ExecutionId, Guid.Empty },
                { ExpressionParameterNames.JobId, JobId }
            };
            return await Expression.EvaluateAsync(parameters);
        }

        return ParameterValue;
    }
}
