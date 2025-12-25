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
        WorkspaceId = other.WorkspaceId;
        WorkspaceName = other.WorkspaceName;
        ItemType = other.ItemType;
        ItemId = other.ItemId;
        ItemName = other.ItemName;
        AzureCredentialId = other.AzureCredentialId;
        AzureCredential = other.AzureCredential;
        StepParameters = other.StepParameters
            .Select(p => new FabricStepParameter(p, this, targetJob))
            .ToList();
    }
    
    [Required]
    public Guid WorkspaceId { get; set; }
    
    [MaxLength(250)]
    public string? WorkspaceName { get; set; }
    
    public FabricItemType ItemType { get; set; }
    
    [Required]
    public Guid ItemId { get; set; }
    
    [MaxLength(250)]
    public string? ItemName { get; set; }
    
    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }
    
    [Required]
    public Guid AzureCredentialId { get; set; }
    
    [JsonIgnore]
    public AzureCredential? AzureCredential { get; set; }
    
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