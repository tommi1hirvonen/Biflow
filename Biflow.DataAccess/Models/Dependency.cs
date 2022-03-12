using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class Dependency
{
    [Required]
    public Guid StepId { get; set; }

    public Step Step { get; set; } = null!;

    [Required]
    public Guid DependantOnStepId { get; set; }

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
