using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class DbtStep : Step, IHasTimeout
{
    [JsonConstructor]
    public DbtStep() : base(StepType.Dbt)
    {
    }

    public DbtStep(DbtStep other, Job? targetJob) : base(other, targetJob)
    {
        DbtJobId = other.DbtJobId;
        DbtJobName = other.DbtJobName;
        TimeoutMinutes = other.TimeoutMinutes;
        DbtAccountId = other.DbtAccountId;
        DbtAccount = other.DbtAccount;
    }

    [Required]
    public long DbtJobId { get; set; }

    [MaxLength(500)]
    public string? DbtJobName
    {
        get;
        set => field = value?[..int.Min(value.Length, 500)];
    }

    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    public Guid DbtAccountId { get; set; }

    [JsonIgnore]
    public DbtAccount DbtAccount { get; set; } = null!;

    public override DbtStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override DbtStepExecution ToStepExecution(Execution execution) => new(this, execution);
}
