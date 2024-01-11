using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class SqlStepExecution: StepExecution, IHasTimeout, IHasStepExecutionParameters<SqlStepExecutionParameter>
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
        StepExecutionAttempts = new[] { new SqlStepExecutionAttempt(this) };
    }

    [Column("ConnectionId")]
    public Guid ConnectionId { get; private set; }

    [Display(Name = "SQL statement")]
    public string SqlStatement { get; private set; }

    [Display(Name = "Result capture job parameter")]
    public Guid? ResultCaptureJobParameterId { get; private set; }

    [Column(TypeName = "sql_variant")]
    public object? ResultCaptureJobParameterValue { get; set; }

    public ExecutionParameter? ResultCaptureJobParameter { get; set; }

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; private set; }

    public IList<SqlStepExecutionParameter> StepExecutionParameters { get; set; } = null!;

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
    [NotMapped]
    private SqlConnectionInfo? _connection;
}
