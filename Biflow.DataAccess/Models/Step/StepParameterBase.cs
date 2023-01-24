using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("StepParameter")]
[PrimaryKey("ParameterId")]
public abstract class StepParameterBase : DynamicParameter
{
    public StepParameterBase(ParameterType parameterType)
    {
        ParameterType = parameterType;
    }

    [Display(Name = "Step id")]
    [Column("StepId")]
    public Guid StepId { get; set; }

    [Required]
    public ParameterType ParameterType { get; }

    public Guid? InheritFromJobParameterId { get; set; }

    public JobParameter? InheritFromJobParameter { get; set; }

    public abstract Step BaseStep { get; }

    public override async Task<object?> EvaluateAsync()
    {
        if (InheritFromJobParameter is not null)
        {
            return await InheritFromJobParameter.EvaluateAsync();
        }

        if (UseExpression)
        {
            return await Expression.EvaluateAsync();
        }

        return ParameterValue;
    }

    public override string DisplayValue => InheritFromJobParameter switch
    {
        not null => $"{InheritFromJobParameter.DisplayValue?.ToString() ?? "null"} (inherited from job parameter {InheritFromJobParameter.DisplayName})",
        _ => base.DisplayValue
    };

    public override string DisplayValueType => InheritFromJobParameter?.DisplayValueType ?? base.DisplayValueType;

}
