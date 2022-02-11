using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class TabularStep : Step
{
    public TabularStep(string tabularModelName)
        : base(StepType.Tabular)
    {
        TabularModelName = tabularModelName;
    }

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

    public AnalysisServicesConnectionInfo Connection { get; set; } = null!;
}
