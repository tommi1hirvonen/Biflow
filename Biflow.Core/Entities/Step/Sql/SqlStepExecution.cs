using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class SqlStepExecution : StepExecution,
    IHasTimeout,
    IHasStepExecutionParameters<SqlStepExecutionParameter>,
    IHasStepExecutionAttempts<SqlStepExecutionAttempt>
{
    public SqlStepExecution(string stepName, string sqlStatement) : base(stepName, StepType.Sql)
    {
        SqlStatement = sqlStatement;
    }

    public SqlStepExecution(SqlStep step, Execution execution) : base(step, execution)
    {
        SqlStatement = step.SqlStatement;
        ConnectionId = step.ConnectionId;
        ResultCaptureJobParameterId = step.ResultCaptureJobParameterId;
        TimeoutMinutes = step.TimeoutMinutes;
        StepExecutionParameters = step.StepParameters
            .Select(p => new SqlStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new SqlStepExecutionAttempt(this));
    }

    public Guid ConnectionId { get; [UsedImplicitly] private set; }

    [Display(Name = "SQL statement")]
    public string SqlStatement { get; private set; }

    [Display(Name = "Result capture job parameter")]
    public Guid? ResultCaptureJobParameterId { get; private set; }

    public ParameterValue ResultCaptureJobParameterValue { get; set; }

    public ExecutionParameter? ResultCaptureJobParameter { get; init; }

    public double TimeoutMinutes { get; [UsedImplicitly] private set; }

    public IEnumerable<SqlStepExecutionParameter> StepExecutionParameters { get; } = new List<SqlStepExecutionParameter>();

    public override SqlStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new SqlStepExecutionAttempt((SqlStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="SqlConnectionBase"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetConnection(SqlConnectionBase?)"/> will need to have been called first for the <see cref="SqlConnectionBase"/> to be available.
    /// </summary>
    /// <returns><see cref="SqlConnectionBase"/> if it was previously set using <see cref="SetConnection(SqlConnectionBase?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public SqlConnectionBase? GetConnection() => _connection;

    /// <summary>
    /// Set the private <see cref="SqlConnectionBase"/> object used for containing a possible connection reference.
    /// It can be later accessed using <see cref="GetConnection"/>.
    /// </summary>
    /// <param name="connection"><see cref="SqlConnectionBase"/> reference to store.
    /// The ConnectionIds are compared and the value is set only if the ids match.</param>
    public void SetConnection(SqlConnectionBase? connection)
    {
        if (connection is not (MsSqlConnection or SnowflakeConnection or null))
        {
            throw new ArgumentException($"Illegal connection type: {connection.GetType().Name}. Connection must be of type {nameof(MsSqlConnection)} or {nameof(SnowflakeConnection)}.");
        }
        if (connection?.ConnectionId == ConnectionId)
        {
            _connection = connection;
        }
    }

    // Use a field excluded from the EF model to store the connection reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private SqlConnectionBase? _connection;
}
