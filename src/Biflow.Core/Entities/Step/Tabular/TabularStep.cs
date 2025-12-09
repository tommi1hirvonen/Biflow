using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class TabularStep : Step, IHasTimeout
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
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string TabularModelName { get; set; } = "";

    [MaxLength(128)]
    public string? TabularTableName { get; set; }

    [MaxLength(128)]
    public string? TabularPartitionName { get; set; }

    [Required]
    [NotEmptyGuid]
    public Guid ConnectionId { get; set; }

    [JsonIgnore]
    public AnalysisServicesConnection Connection { get; init; } = null!;
    
    public override DisplayStepType DisplayStepType => DisplayStepType.Tabular;

    public override TabularStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new TabularStepExecution(this, execution);
}
