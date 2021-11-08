using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models;

public class Dependency
{
    [Required]
    public Guid StepId { get; set; }

    public Step Step { get; set; } = null!;

    [Required]
    public Guid DependantOnStepId { get; set; }

    public Step DependantOnStep { get; set; } = null!;

    [Display(Name = "Strict dependency")]
    public bool StrictDependency { get; set; }

    [Required]
    [Display(Name = "Created")]
    public DateTimeOffset CreatedDateTime { get; set; }

    [Display(Name = "Created by")]
    public string? CreatedBy { get; set; }
}
