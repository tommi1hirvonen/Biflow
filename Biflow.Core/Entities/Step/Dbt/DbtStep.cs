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
        DbtJob = other.DbtJob;
        TimeoutMinutes = other.TimeoutMinutes;
        DbtAccountId = other.DbtAccountId;
        DbtAccount = other.DbtAccount;
    }

    public DbtJobDetails DbtJob { get; set; } = new()
    {
        Id = 0,
        Name = null, 
        EnvironmentId = 0, 
        EnvironmentName = null, 
        ProjectId = 0,
        ProjectName = null
    };

    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    public Guid DbtAccountId { get; set; }

    [JsonIgnore]
    public DbtAccount DbtAccount { get; set; } = null!;

    public override DbtStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override DbtStepExecution ToStepExecution(Execution execution) => new(this, execution);
}
