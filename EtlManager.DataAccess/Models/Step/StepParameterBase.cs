using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtlManager.DataAccess.Models;

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
    public ParameterType ParameterType { get; private init; }

    public ParameterizedStep Step { get; set; } = null!;

    public Guid? JobParameterId { get; set; }

    public JobParameter? JobParameter { get; set; }
}
