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

    public override async Task<object?> EvaluateAsync()
    {
        if (UseExpression)
        {
            return await Expression.EvaluateAsync();
        }

        return ParameterValue;
    }
}
