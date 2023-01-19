using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class AgentJobStep : Step, IHasConnection<SqlConnectionInfo>, IHasTimeout
{
    public AgentJobStep(string agentJobName) : base(StepType.AgentJob)
    {
        AgentJobName = agentJobName;
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

    public SqlConnectionInfo Connection { get; set; } = null!;
}
