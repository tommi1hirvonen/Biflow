using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class SqlStepExecution : StepExecution
{
    public SqlStepExecution(string stepName, string sqlStatement) : base(stepName, StepType.Sql)
    {
        SqlStatement = sqlStatement;
    }

    [Column("ConnectionId")]
    public Guid ConnectionId { get; set; }

    public SqlConnectionInfo Connection { get; set; } = null!;

    [Display(Name = "SQL statement")]
    public string SqlStatement { get; set; }

    [Display(Name = "Result capture job parameter")]
    public Guid? ResultCaptureJobParameterId { get; set; }

    [Column(TypeName = "sql_variant")]
    public object? ResultCaptureJobParameterValue { get; set; }

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; set; }

    public override bool SupportsParameterization => true;
}
