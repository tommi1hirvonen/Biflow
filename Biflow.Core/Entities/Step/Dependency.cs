using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class Dependency : IAuditable
{
    public Dependency()
    {
    }

    internal Dependency(Dependency other, Step step)
    {
        StepId = step.StepId;
        Step = step;
        DependantOnStepId = other.DependantOnStepId;
        DependantOnStep = other.DependantOnStep;
        DependencyType = other.DependencyType;
    }

    public Guid StepId { get; init; }

    [JsonIgnore]
    public Step Step { get; set; } = null!;

    public Guid DependantOnStepId { get; init; }

    [JsonIgnore]
    public Step DependantOnStep { get; set; } = null!;

    [Display(Name = "Type")]
    public DependencyType DependencyType { get; set; }

    [Display(Name = "Created")]
    public DateTimeOffset CreatedOn { get; set; }

    [Display(Name = "Created by")]
    [MaxLength(250)]
    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModifiedOn { get; set; }

    [MaxLength(250)]
    public string? LastModifiedBy { get; set; }

    [JsonIgnore]
    public bool IsCandidateForRemoval { get; set; }

    [JsonIgnore]
    public bool IsNewAddition { get; init; }
}
