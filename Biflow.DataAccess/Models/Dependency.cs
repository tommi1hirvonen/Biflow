using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Dependency")]
[PrimaryKey("StepId", "DependantOnStepId")]
public class Dependency
{
    public Dependency(Guid stepId, Guid dependantOnStepId)
    {
        StepId = stepId;
        DependantOnStepId = dependantOnStepId;
    }

    public Guid StepId { get; }

    public Step Step { get; set; } = null!;

    public Guid DependantOnStepId { get; }

    public Step DependantOnStep { get; set; } = null!;

    [Display(Name = "Type")]
    public DependencyType DependencyType { get; set; }

    [Required]
    [Display(Name = "Created")]
    public DateTimeOffset CreatedDateTime { get; set; }

    [Display(Name = "Created by")]
    public string? CreatedBy { get; set; }

    [NotMapped]
    public bool IsCandidateForRemoval { get; set; } = false;

    [NotMapped]
    public bool IsNewAddition { get; set; } = false;
}
