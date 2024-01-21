using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Biflow.Core.Constants;

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
