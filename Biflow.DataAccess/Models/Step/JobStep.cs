using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class JobStep : Step, IHasStepParameters<JobStepParameter>
{
    public JobStep(Guid jobId) : base(StepType.Job, jobId) { }

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

    public Job JobToExecute { get; set; } = null!;

    [ValidateComplexType]
    public IList<JobStepParameter> StepParameters { get; set; } = null!;

    public IList<Tag> TagFilters { get; set; } = null!;

    internal override JobStep Copy(Job? targetJob = null) => new(this, targetJob);

    internal override StepExecution ToStepExecution(Execution execution) => new JobStepExecution(this, execution);
}
