using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public class JobStep : Step
{
    public JobStep() : base(StepType.Job) { }

    [Display(Name = "Job to execute")]
    [Required]
    public Guid? JobToExecuteId { get; set; }

    [Display(Name = "Synchronized")]
    [Required]
    public bool JobExecuteSynchronized { get; set; }

    public Job JobToExecute { get; set; } = null!;
}
