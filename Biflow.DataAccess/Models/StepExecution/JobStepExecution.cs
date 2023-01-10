using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class JobStepExecution : ParameterizedStepExecution
{
    public JobStepExecution(string stepName) : base(stepName, StepType.Job)
    {
    }

    [Display(Name = "Job to execute")]
    public Guid JobToExecuteId { get; set; }

    [Display(Name = "Synchronized")]
    public bool JobExecuteSynchronized { get; set; }
}
