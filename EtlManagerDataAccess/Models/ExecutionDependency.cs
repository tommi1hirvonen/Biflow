using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models;

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
