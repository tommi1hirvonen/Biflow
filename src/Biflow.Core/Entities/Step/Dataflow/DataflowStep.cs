using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class DataflowStep : Step, IHasTimeout
{
    [JsonConstructor]
    public DataflowStep() : base(StepType.Dataflow) { }

    private DataflowStep(DataflowStep other, Job? targetJob) : base(other, targetJob)
    {
        FabricWorkspaceId = other.FabricWorkspaceId;
        FabricWorkspace = other.FabricWorkspace;
        DataflowId = other.DataflowId;
        DataflowName = other.DataflowName;
        TimeoutMinutes = other.TimeoutMinutes;
    }

    [MaxLength(36)]
    [MinLength(36)]
    [Required]
    [NotEmptyGuid]
    public string DataflowId { get; set; } = "";
    
    [MaxLength(250)]
    public string? DataflowName { get; set; }
    
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    public Guid FabricWorkspaceId { get; set; }
    
    [JsonIgnore]
    public FabricWorkspace? FabricWorkspace { get; set; }

    [JsonIgnore]
    public override DisplayStepType DisplayStepType => DisplayStepType.Dataflow;

    public override DataflowStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new DataflowStepExecution(this, execution);
}