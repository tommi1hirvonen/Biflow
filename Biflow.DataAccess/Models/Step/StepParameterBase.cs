using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("StepParameter")]
[PrimaryKey("ParameterId")]
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

    public Guid? InheritFromJobParameterId { get; set; }

    public JobParameter? InheritFromJobParameter { get; set; }

    public abstract Step BaseStep { get; }
}
