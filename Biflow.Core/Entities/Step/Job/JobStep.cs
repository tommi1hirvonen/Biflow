using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class JobStep : Step, IHasStepParameters<JobStepParameter>
{
    [JsonConstructor]
    public JobStep() : base(StepType.Job) { }

    private JobStep(JobStep other, Job? targetJob) : base(other, targetJob)
    {
        JobToExecuteId = other.JobToExecuteId;
        JobToExecute = other.JobToExecute;
        JobExecuteSynchronized = other.JobExecuteSynchronized;
        TagFilters = other.TagFilters;
        StepParameters = other.StepParameters
            .Select(p => new JobStepParameter(p, this, targetJob))
            .ToList();
    }

    [Display(Name = "Job to execute")]
    [Required]
    public Guid? JobToExecuteId { get; set; }

    [Display(Name = "Synchronized")]
    [Required]
    public bool JobExecuteSynchronized { get; set; }

    [JsonIgnore]
    public Job JobToExecute { get; set; } = null!;

    [ValidateComplexType]
    public IList<JobStepParameter> StepParameters { get; } = new List<JobStepParameter>();

    public ICollection<StepTag> TagFilters { get; init; } = new List<StepTag>();

    public override JobStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new JobStepExecution(this, execution);
}
