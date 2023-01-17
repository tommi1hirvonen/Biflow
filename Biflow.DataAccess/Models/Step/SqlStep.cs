using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class SqlStep : Step, IHasConnection<SqlConnectionInfo>
{
    public SqlStep() : base(StepType.Sql) { }

    [Display(Name = "SQL statement")]
    [Required]
    public string? SqlStatement { get; set; }

    [Column("ConnectionId")]
    [Required]
    public Guid? ConnectionId { get; set; }

    [Display(Name = "Result capture job parameter")]
    public Guid? ResultCaptureJobParameterId { get; set; }

    public override bool SupportsParameterization => true;

    public SqlConnectionInfo Connection { get; set; } = null!;
}
