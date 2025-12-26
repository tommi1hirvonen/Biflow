using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class FabricStep : Step, IHasTimeout, IHasStepParameters<FabricStepParameter>
{
    [JsonConstructor]
    public FabricStep() : base(StepType.Fabric) { }

    private FabricStep(FabricStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        ItemType = other.ItemType;
        ItemId = other.ItemId;
        ItemName = other.ItemName;
        FabricWorkspaceId = other.FabricWorkspaceId;
        FabricWorkspace = other.FabricWorkspace;
        StepParameters = other.StepParameters
            .Select(p => new FabricStepParameter(p, this, targetJob))
            .ToList();
    }
    
    public FabricItemType ItemType { get; set; }
    
    [Required]
    public Guid ItemId { get; set; }
    
    [Required]
    [MaxLength(250)]
    public string ItemName { get; set; } = string.Empty;
    
    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }
    
    [Required]
    public Guid FabricWorkspaceId { get; set; }
    
    [JsonIgnore]
    public FabricWorkspace? FabricWorkspace { get; set; }
    
    [ValidateComplexType]
    [JsonInclude]
    public IList<FabricStepParameter> StepParameters { get; private set; } = new List<FabricStepParameter>();
    
    [JsonIgnore]
    public override DisplayStepType DisplayStepType => ItemType switch
    {
        FabricItemType.Notebook => DisplayStepType.FabricNotebook,
        FabricItemType.DataPipeline => DisplayStepType.FabricPipeline,
        _ => DisplayStepType.Fabric
    };
    
    public override FabricStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new FabricStepExecution(this, execution);
}