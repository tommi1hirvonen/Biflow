using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionDependency")]
[PrimaryKey("ExecutionId", "StepId", "DependantOnStepId")]
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

    public StepExecution? DependantOnStepExecution { get; set; }
}
