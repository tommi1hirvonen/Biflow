using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class AgentJobStep : Step, IHasConnection<SqlConnectionInfo>, IHasTimeout
{
    [JsonConstructor]
    public AgentJobStep(Guid jobId, string agentJobName) : base(StepType.AgentJob, jobId)
    {
        AgentJobName = agentJobName;
    }

    private AgentJobStep(AgentJobStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        AgentJobName = other.AgentJobName;
        ConnectionId = other.ConnectionId;
        Connection = other.Connection;
    }

    [Column("TimeoutMinutes")]
    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Display(Name = "Agent job name")]
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string AgentJobName { get; set; }

    [Column("ConnectionId")]
    [Required]
    public Guid? ConnectionId { get; set; }

    [JsonIgnore]
    public SqlConnectionInfo Connection { get; set; } = null!;

    internal override AgentJobStep Copy(Job? targetJob = null) => new(this, targetJob);

    internal override StepExecution ToStepExecution(Execution execution) => new AgentJobStepExecution(this, execution);
}
