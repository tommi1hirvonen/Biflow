using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class ExecutionDependency
{
    public Guid ExecutionId { get; set; }

    [Required]
    public Guid StepId { get; set; }

    [Required]
    public Guid DependantOnStepId { get; set; }

    [Display(Name = "Strict dependency")]
    public bool StrictDependency { get; set; }

    public StepExecution StepExecution { get; set; } = null!;

    public StepExecution DependantOnStepExecution { get; set; } = null!;
}
