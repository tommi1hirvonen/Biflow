using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class JobStep : Step, IHasStepParameters<JobStepParameter>
{
    public JobStep() : base(StepType.Job) { }

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

    public override StepExecution ToStepExecution(Execution execution) => new JobStepExecution(this, execution);
}
