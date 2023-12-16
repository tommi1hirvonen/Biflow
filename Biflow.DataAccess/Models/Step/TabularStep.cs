using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class TabularStep : Step, IHasConnection<AnalysisServicesConnectionInfo>, IHasTimeout
{
    [JsonConstructor]
    public TabularStep(Guid jobId, string tabularModelName)
        : base(StepType.Tabular, jobId)
    {
        TabularModelName = tabularModelName;
    }

    private TabularStep(TabularStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        TabularModelName = other.TabularModelName;
        TabularTableName = other.TabularTableName;
        TabularPartitionName = other.TabularPartitionName;
        ConnectionId = other.ConnectionId;
        Connection = other.Connection;
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

    [JsonIgnore]
    public AnalysisServicesConnectionInfo Connection { get; set; } = null!;

    internal override TabularStep Copy(Job? targetJob = null) => new(this, targetJob);

    internal override StepExecution ToStepExecution(Execution execution) => new TabularStepExecution(this, execution);
}
