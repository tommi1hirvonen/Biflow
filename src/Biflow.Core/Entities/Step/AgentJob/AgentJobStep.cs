using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class AgentJobStep : Step, IHasSqlConnection, IHasTimeout
{
    [JsonConstructor]
    public AgentJobStep() : base(StepType.AgentJob)
    {
    }

    private AgentJobStep(AgentJobStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        AgentJobName = other.AgentJobName;
        ConnectionId = other.ConnectionId;
        Connection = other.Connection;
    }

    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string AgentJobName { get; set; } = "";

    [Required]
    [NotEmptyGuid]
    public Guid ConnectionId { get; set; }

    [JsonIgnore]
    public MsSqlConnection Connection { get; init; } = null!;

    [JsonIgnore]
    SqlConnectionBase IHasSqlConnection.Connection => Connection;

    public override DisplayStepType DisplayStepType => DisplayStepType.AgentJob;

    public override AgentJobStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new AgentJobStepExecution(this, execution);
}
