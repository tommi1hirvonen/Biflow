using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManager.DataAccess.Models;

public class AgentJobStepExecution : StepExecution
{
    public AgentJobStepExecution(string stepName, string agentJobName) : base(stepName, StepType.AgentJob)
    {
        AgentJobName = agentJobName;
    }

    [Display(Name = "Agent job name")]
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string AgentJobName { get; set; }

    [Column("TimeoutMinutes")]
    public int TimeoutMinutes { get; set; }

    [Column("ConnectionId")]
    [Required]
    public Guid? ConnectionId { get; set; }

    public SqlConnectionInfo Connection { get; set; } = null!;
}
