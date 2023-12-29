using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class AgentJobStepExecution : StepExecution, IHasTimeout, IHasConnection<SqlConnectionInfo?>
{
    public AgentJobStepExecution(string stepName, string agentJobName) : base(stepName, StepType.AgentJob)
    {
        AgentJobName = agentJobName;
    }

    public AgentJobStepExecution(AgentJobStep step, Execution execution) : base(step, execution)
    {
        AgentJobName = step.AgentJobName;
        TimeoutMinutes = step.TimeoutMinutes;
        ConnectionId = step.ConnectionId;

        StepExecutionAttempts = new[] { new AgentJobStepExecutionAttempt(this) };
    }

    [Display(Name = "Agent job name")]
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string AgentJobName { get; private set; }

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; private set; }

    [Column("ConnectionId")]
    [Required]
    public Guid ConnectionId { get; private set; }

    [NotMapped]
    public SqlConnectionInfo? Connection { get; set; }

}
