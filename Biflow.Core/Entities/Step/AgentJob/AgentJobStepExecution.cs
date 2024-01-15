using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class AgentJobStepExecution : StepExecution, IHasTimeout
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

        StepExecutionAttempts.Add(new AgentJobStepExecutionAttempt(this));
    }

    [Display(Name = "Agent job name")]
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string AgentJobName { get; private set; }

    public double TimeoutMinutes { get; private set; }

    [Required]
    public Guid ConnectionId { get; private set; }

    /// <summary>
    /// Get the <see cref="SqlConnectionInfo"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetConnection(SqlConnectionInfo?)"/> will need to have been called first for the <see cref="SqlConnectionInfo"/> to be available.
    /// </summary>
    /// <returns><see cref="SqlConnectionInfo"/> if it was previously set using <see cref="SetConnection(SqlConnectionInfo?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public SqlConnectionInfo? GetConnection() => _connection;

    /// <summary>
    /// Set the private <see cref="SqlConnectionInfo"/> object used for containing a possible connection reference.
    /// It can be later accessed using <see cref="GetConnection"/>.
    /// </summary>
    /// <param name="connection"><see cref="SqlConnectionInfo"/> reference to store.
    /// The ConnectionIds are compared and the value is set only if the ids match.</param>
    public void SetConnection(SqlConnectionInfo? connection)
    {
        if (connection?.ConnectionId == ConnectionId)
        {
            _connection = connection;
        }
    }

    // Use a field excluded from the EF model to store the connection reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private SqlConnectionInfo? _connection;

}
