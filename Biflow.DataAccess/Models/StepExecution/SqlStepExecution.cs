using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class SqlStepExecution : StepExecution, IHasTimeout, IHasStepExecutionParameters<SqlStepExecutionParameter>
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

    public SqlConnectionInfo Connection { get; set; } = null!;

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
}
