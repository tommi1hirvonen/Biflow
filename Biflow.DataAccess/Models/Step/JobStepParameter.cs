using System.ComponentModel.DataAnnotations;

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

        // The target job is set, the JobParameter is not null and the target job has a parameter with a matching name.
        if (job is not null && job.JobParameters.FirstOrDefault(p => p.ParameterName == other.AssignToJobParameter.ParameterName) is JobParameter parameter)
        {
            AssignToJobParameterId = parameter.ParameterId;
            AssignToJobParameter = parameter;
        }
        // The target job has no parameter with a mathing name, so add one.
        else if (job is not null)
        {
            var newParameter = new JobParameter(other.AssignToJobParameter, job);
            AssignToJobParameterId = newParameter.ParameterId;
            AssignToJobParameter = newParameter;
        }
        else
        {
            AssignToJobParameterId = other.AssignToJobParameterId;
            AssignToJobParameter = other.AssignToJobParameter;
        }
    }

    [Required]
    public Guid AssignToJobParameterId { get; set; }

    public JobParameter AssignToJobParameter { get; set; } = null!;

    public JobStep Step { get; set; } = null!;

    public override Step BaseStep => Step;

}
