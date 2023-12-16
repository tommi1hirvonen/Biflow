using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class JobStepParameter : StepParameterBase
{
    public JobStepParameter(Guid assignToJobParameterId) : base(ParameterType.Job)
    {
        AssignToJobParameterId = assignToJobParameterId;
    }

    internal JobStepParameter(JobStepParameter other, JobStep step, Job? job) : base(other, step, job)
    {
        Step = step;
        AssignToJobParameterId = other.AssignToJobParameterId;
        AssignToJobParameter = other.AssignToJobParameter;
    }

    [Required]
    public Guid AssignToJobParameterId { get; set; }

    [JsonIgnore]
    public JobParameter AssignToJobParameter { get; set; } = null!;

    [JsonIgnore]
    public JobStep Step { get; set; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;

}
