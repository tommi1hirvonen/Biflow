using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("JobParameter")]
[PrimaryKey("ParameterId")]
public class JobParameter : DynamicParameter
{
    [Display(Name = "Job")]
    [Column("JobId")]
    public Guid JobId { get; set; }

    public Job Job { get; set; } = null!;

    public ICollection<StepParameterBase> InheritingStepParameters { get; set; } = null!;

    public ICollection<JobStepParameter> AssigningStepParameters { get; set; } = null!;

    public ICollection<StepParameterExpressionParameter> InheritingStepParameterExpressionParameters { get; set; } = null!;

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
