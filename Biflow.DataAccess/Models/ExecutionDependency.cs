using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class ExecutionDependency
{
    public Guid ExecutionId { get; set; }

    [Required]
    public Guid StepId { get; set; }

    [Required]
    public Guid DependantOnStepId { get; set; }

    [Display(Name = "Type")]
    public DependencyType DependencyType { get; set; }

    public StepExecution StepExecution { get; set; } = null!;

    public StepExecution DependantOnStepExecution { get; set; } = null!;
}
