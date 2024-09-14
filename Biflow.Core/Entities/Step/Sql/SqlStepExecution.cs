using Biflow.Core.Interfaces;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

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

    public Guid ConnectionId { get; private set; }

    [Display(Name = "SQL statement")]
    public string SqlStatement { get; private set; }

    [Display(Name = "Result capture job parameter")]
    public Guid? ResultCaptureJobParameterId { get; private set; }

    public ParameterValue ResultCaptureJobParameterValue { get; set; } = new();

    public ExecutionParameter? ResultCaptureJobParameter { get; set; }

    public double TimeoutMinutes { get; private set; }

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
    /// Get the <see cref="ConnectionBase"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetConnection(ConnectionBase?)"/> will need to have been called first for the <see cref="ConnectionBase"/> to be available.
    /// </summary>
    /// <returns><see cref="MsSqlConConnectionBasenection"/> if it was previously set using <see cref="SetConnection(ConnectionBase?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public ConnectionBase? GetConnection() => _connection;

    /// <summary>
    /// Set the private <see cref="ConnectionBase"/> object used for containing a possible connection reference.
    /// It can be later accessed using <see cref="GetConnection"/>.
    /// </summary>
    /// <param name="connection"><see cref="ConnectionBase"/> reference to store.
    /// The ConnectionIds are compared and the value is set only if the ids match.</param>
    public void SetConnection(ConnectionBase? connection)
    {
        if (!(connection is MsSqlConnection or SnowflakeConnection))
        {
            throw new ArgumentException($"Unallowed connection type: {connection?.GetType().Name}. Connection must be of type {typeof(MsSqlConnection).Name} or {typeof(SnowflakeConnection).Name}.");
        }
        if (connection?.ConnectionId == ConnectionId)
        {
            _connection = connection;
        }
    }

    // Use a field excluded from the EF model to store the connection reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private ConnectionBase? _connection;
}
