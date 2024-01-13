using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

public class TabularStepExecution : StepExecution, IHasTimeout
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

        StepExecutionAttempts = new[] { new TabularStepExecutionAttempt(this) };
    }

    [Display(Name = "Model name")]
    [Required]
    [MaxLength(128)]
    public string TabularModelName { get; private set; }

    [Display(Name = "Table name")]
    [MaxLength(128)]
    public string? TabularTableName { get; private set; }

    [Display(Name = "Partition name")]
    [MaxLength(128)]
    public string? TabularPartitionName { get; private set; }

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; private set; }

    [Column("ConnectionId")]
    [Required]
    public Guid ConnectionId { get; private set; }

    /// <summary>
    /// Get the <see cref="AnalysisServicesConnectionInfo"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetConnection(AnalysisServicesConnectionInfo?)"/> will need to have been called first for the <see cref="AnalysisServicesConnectionInfo"/> to be available.
    /// </summary>
    /// <returns><see cref="AnalysisServicesConnectionInfo"/> if it was previously set using <see cref="SetConnection(AnalysisServicesConnectionInfo?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public AnalysisServicesConnectionInfo? GetConnection() => _connection;

    /// <summary>
    /// Set the private <see cref="AnalysisServicesConnectionInfo"/> object used for containing a possible connection reference.
    /// It can be later accessed using <see cref="GetConnection"/>.
    /// </summary>
    /// <param name="connection"><see cref="AnalysisServicesConnectionInfo"/> reference to store.
    /// The ConnectionIds are compared and the value is set only if the ids match.</param>
    public void SetConnection(AnalysisServicesConnectionInfo? connection)
    {
        if (connection?.ConnectionId == ConnectionId)
        {
            _connection = connection;
        }
    }

    // Use a field excluded from the EF model to store the connection reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    [NotMapped]
    private AnalysisServicesConnectionInfo? _connection;
}
