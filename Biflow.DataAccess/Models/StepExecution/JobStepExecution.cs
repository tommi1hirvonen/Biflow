using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class JobStepExecution : StepExecution, IHasStepExecutionParameters<JobStepExecutionParameter>
{
    public JobStepExecution(string stepName) : base(stepName, StepType.Job)
    {
    }

    [Display(Name = "Job to execute")]
    public Guid JobToExecuteId { get; private set; }

    [Display(Name = "Synchronized")]
    public bool JobExecuteSynchronized { get; private set; }

    public IList<JobStepExecutionParameter> StepExecutionParameters { get; set; } = null!;

    public IList<Tag> TagFilters { get; set; } = null!;
}
