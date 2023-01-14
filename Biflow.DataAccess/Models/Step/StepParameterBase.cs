using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("StepParameter")]
public abstract class StepParameterBase : ParameterBase
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

    public Step Step { get; set; } = null!;

    public Guid? InheritFromJobParameterId { get; set; }

    public JobParameter? InheritFromJobParameter { get; set; }
}
