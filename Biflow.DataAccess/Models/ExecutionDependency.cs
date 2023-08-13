using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionDependency")]
[PrimaryKey("ExecutionId", "StepId", "DependantOnStepId")]
public class ExecutionDependency
{
    public Guid ExecutionId { get; private set; }

    public Guid StepId { get; private set; }

    public Guid DependantOnStepId { get; private set; }

    [Display(Name = "Type")]
    public DependencyType DependencyType { get; private set; }

    public StepExecution StepExecution { get; set; } = null!;

    public StepExecution? DependantOnStepExecution { get; set; }
}
