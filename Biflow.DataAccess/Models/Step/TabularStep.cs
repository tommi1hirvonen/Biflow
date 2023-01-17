using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class TabularStep : Step, IHasConnection<AnalysisServicesConnectionInfo>, IHasTimeout
{
    public TabularStep(string tabularModelName)
        : base(StepType.Tabular)
    {
        TabularModelName = tabularModelName;
    }

    [Column("TimeoutMinutes")]
    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Display(Name = "Tabular model name")]
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string TabularModelName { get; set; }

    [Display(Name = "Table name")]
    [MaxLength(128)]
    public string? TabularTableName { get; set; }

    [Display(Name = "Partition name")]
    [MaxLength(128)]
    public string? TabularPartitionName { get; set; }

    [Column("ConnectionId")]
    [Required]
    public Guid? ConnectionId { get; set; }

    public override bool SupportsParameterization => false;

    public AnalysisServicesConnectionInfo Connection { get; set; } = null!;
}
