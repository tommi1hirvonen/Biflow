using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class ScdStep : Step, IHasTimeout
{
    [JsonConstructor]
    public ScdStep() : base(StepType.Scd)
    {
    }

    public ScdStep(ScdStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        ScdTableId = other.ScdTableId;
        ScdTable = other.ScdTable;
    }
    
    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    public Guid? ScdTableId { get; set; }

    [JsonIgnore]
    public ScdTable ScdTable { get; set; } = null!;

    public override ScdStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override ScdStepExecution ToStepExecution(Execution execution) => new(this, execution);
}
