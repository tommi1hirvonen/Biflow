using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

    internal Dependency(Dependency other, Step step)
    {
        StepId = step.StepId;
        Step = step;
        DependantOnStepId = other.DependantOnStepId;
        DependantOnStep = other.DependantOnStep;
        DependencyType = other.DependencyType;
    }

    public Guid StepId { get; }

    [JsonIgnore]
    public Step Step { get; set; } = null!;

    public Guid DependantOnStepId { get; }

    [JsonIgnore]
    public Step DependantOnStep { get; set; } = null!;

    [Display(Name = "Type")]
    public DependencyType DependencyType { get; set; }

    [Required]
    [Display(Name = "Created")]
    public DateTimeOffset CreatedDateTime { get; set; }

    [Display(Name = "Created by")]
    public string? CreatedBy { get; set; }

    [NotMapped]
    [JsonIgnore]
    public bool IsCandidateForRemoval { get; set; } = false;

    [NotMapped]
    [JsonIgnore]
    public bool IsNewAddition { get; set; } = false;
}
