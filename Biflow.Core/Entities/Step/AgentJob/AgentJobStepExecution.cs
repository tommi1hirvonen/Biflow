using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class AgentJobStepExecution : StepExecution, IHasTimeout, IHasStepExecutionAttempts<AgentJobStepExecutionAttempt>
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

        AddAttempt(new AgentJobStepExecutionAttempt(this));
    }

    [Display(Name = "Agent job name")]
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string AgentJobName { get; private set; }

    public double TimeoutMinutes { get; private set; }

    [Required]
    public Guid ConnectionId { get; private set; }

    public override AgentJobStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new AgentJobStepExecutionAttempt((AgentJobStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="MsSqlConnection"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetConnection(MsSqlConnection?)"/> will need to have been called first for the <see cref="MsSqlConnection"/> to be available.
    /// </summary>
    /// <returns><see cref="MsSqlConnection"/> if it was previously set using <see cref="SetConnection(MsSqlConnection?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public MsSqlConnection? GetConnection() => _connection;

    /// <summary>
    /// Set the private <see cref="MsSqlConnection"/> object used for containing a possible connection reference.
    /// It can be later accessed using <see cref="GetConnection"/>.
    /// </summary>
    /// <param name="connection"><see cref="MsSqlConnection"/> reference to store.
    /// The ConnectionIds are compared and the value is set only if the ids match.</param>
    public void SetConnection(MsSqlConnection? connection)
    {
        if (connection?.ConnectionId == ConnectionId)
        {
            _connection = connection;
        }
    }

    // Use a field excluded from the EF model to store the connection reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private MsSqlConnection? _connection;

}
