using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class SqlStep : Step, IHasConnection<SqlConnectionInfo>, IHasTimeout, IHasStepParameters<SqlStepParameter>
{
    public SqlStep() : base(StepType.Sql) { }

    [Column("TimeoutMinutes")]
    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Display(Name = "SQL statement")]
    [Required]
    public string? SqlStatement { get; set; }

    [Column("ConnectionId")]
    [Required]
    public Guid? ConnectionId { get; set; }

    [Display(Name = "Result capture job parameter")]
    public Guid? ResultCaptureJobParameterId { get; set; }

    public SqlConnectionInfo Connection { get; set; } = null!;

    public IList<SqlStepParameter> StepParameters { get; set; } = null!;
}
