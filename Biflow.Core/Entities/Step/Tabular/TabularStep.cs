using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class TabularStep : Step, IHasConnection<AnalysisServicesConnectionInfo>, IHasTimeout
{
    [JsonConstructor]
    public TabularStep() : base(StepType.Tabular)
    {
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

    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Display(Name = "Tabular model name")]
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string TabularModelName { get; set; } = "";

    [Display(Name = "Table name")]
    [MaxLength(128)]
    public string? TabularTableName { get; set; }

    [Display(Name = "Partition name")]
    [MaxLength(128)]
    public string? TabularPartitionName { get; set; }

    [Required]
    [NotEmptyGuid]
    public Guid ConnectionId { get; set; }

    [JsonIgnore]
    public AnalysisServicesConnectionInfo Connection { get; set; } = null!;

    public override TabularStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new TabularStepExecution(this, execution);
}
