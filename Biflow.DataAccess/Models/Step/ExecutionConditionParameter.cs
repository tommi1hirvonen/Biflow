using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("StepConditionParameter")]
[PrimaryKey("ParameterId")]
public class ExecutionConditionParameter : ParameterBase, IAsyncEvaluable
{
    [Display(Name = "Step id")]
    public Guid StepId { get; set; }

    public Step Step { get; set; } = null!;

    public Guid? JobParameterId { get; set; }

    public JobParameter? JobParameter { get; set; }

    public async Task<object?> EvaluateAsync()
    {
        if (JobParameter is not null)
        {
            return await JobParameter.EvaluateAsync();
        }

        return ParameterValue;
    }

    public override string DisplayValue => JobParameter switch
    {
        not null => $"{JobParameter.DisplayValue?.ToString() ?? "null"} (inherited from job parameter {JobParameter.DisplayName})",
        _ => base.DisplayValue
    };

    public override string DisplayValueType => JobParameter?.DisplayValueType ?? base.DisplayValueType;
}
