using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Biflow.Core.Attributes.Validation;

namespace Biflow.Core.Entities;

public class DatasetStep : Step
{
    [JsonConstructor]
    public DatasetStep() : base(StepType.Dataset) { }

    private DatasetStep(DatasetStep other, Job? targetJob) : base(other, targetJob)
    {
        FabricWorkspaceId = other.FabricWorkspaceId;
        FabricWorkspace = other.FabricWorkspace;
        DatasetId = other.DatasetId;
        DatasetName = other.DatasetName;
    }

    [MaxLength(36)]
    [MinLength(36)]
    [Required]
    [NotEmptyGuid]
    public string DatasetId { get; set; } = "";

    [MaxLength(250)]
    [Required]
    public string DatasetName { get; set; } = "";

    [Required]
    public Guid FabricWorkspaceId { get; set; }
    
    [JsonIgnore]
    public FabricWorkspace? FabricWorkspace { get; set; }
    
    [JsonIgnore]
    public override DisplayStepType DisplayStepType => DisplayStepType.Dataset;

    public override DatasetStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new DatasetStepExecution(this, execution);
}
