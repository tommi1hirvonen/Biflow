using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManager.DataAccess.Models;

public class SqlStepExecution : ParameterizedStepExecution
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
    public int TimeoutMinutes { get; set; }
}
