using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class TabularStepExecution : StepExecution, IHasTimeout, IHasStepExecutionAttempts<TabularStepExecutionAttempt>
{
    public TabularStepExecution(string stepName, string tabularModelName)
        : base(stepName, StepType.Tabular)
    {
        TabularModelName = tabularModelName;
    }

    public TabularStepExecution(TabularStep step, Execution execution) : base(step, execution)
    {
        TabularModelName = step.TabularModelName;
        TabularTableName = step.TabularTableName;
        TabularPartitionName = step.TabularPartitionName;
        TimeoutMinutes = step.TimeoutMinutes;
        ConnectionId = step.ConnectionId;

        AddAttempt(new TabularStepExecutionAttempt(this));
    }

    [Required]
    [MaxLength(128)]
    public string TabularModelName { get; private set; }

    [MaxLength(128)]
    public string? TabularTableName { get; private set; }

    [MaxLength(128)]
    public string? TabularPartitionName { get; private set; }

    public double TimeoutMinutes { get; [UsedImplicitly] private set; }

    [Required]
    public Guid ConnectionId { get; [UsedImplicitly] private set; }
    
    public override DisplayStepType DisplayStepType => DisplayStepType.Tabular;

    public override TabularStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new TabularStepExecutionAttempt((TabularStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="AnalysisServicesConnection"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetConnection(AnalysisServicesConnection?)"/> will need to have been called first for the <see cref="AnalysisServicesConnection"/> to be available.
    /// </summary>
    /// <returns><see cref="AnalysisServicesConnection"/> if it was previously set using <see cref="SetConnection(AnalysisServicesConnection?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public AnalysisServicesConnection? GetConnection() => _connection;

    /// <summary>
    /// Set the private <see cref="AnalysisServicesConnection"/> object used for containing a possible connection reference.
    /// It can be later accessed using <see cref="GetConnection"/>.
    /// </summary>
    /// <param name="connection"><see cref="AnalysisServicesConnection"/> reference to store.
    /// The ConnectionIds are compared and the value is set only if the ids match.</param>
    public void SetConnection(AnalysisServicesConnection? connection)
    {
        if (connection?.ConnectionId == ConnectionId)
        {
            _connection = connection;
        }
    }

    // Use a field excluded from the EF model to store the connection reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private AnalysisServicesConnection? _connection;
}
