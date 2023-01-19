using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class JobStepParameter : StepParameterBase
{
    public JobStepParameter(Guid assignToJobParameterId) : base(ParameterType.Job)
    {
        AssignToJobParameterId = assignToJobParameterId;
    }

    [Required]
    public Guid AssignToJobParameterId { get; set; }

    public JobParameter AssignToJobParameter { get; set; } = null!;

    public JobStep Step { get; set; } = null!;

    public override Step BaseStep => Step;

}
