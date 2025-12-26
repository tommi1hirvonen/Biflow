using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class FabricWorkspace
{
    public Guid FabricWorkspaceId { get; [UsedImplicitly] init; }
    
    /// <summary>
    /// The internal name for the Fabric workspace. This is used as the workspace name in the UI. 
    /// </summary>
    [Required]
    [MaxLength(250)]
    public string FabricWorkspaceName { get; set; } = string.Empty;
    
    /// <summary>
    /// The id of the workspace in Microsoft Fabric
    /// </summary>
    [Required]
    public Guid WorkspaceId { get; set; }
    
    /// <summary>
    /// ID of the Azure credential to use for this workspace.
    /// </summary>
    [Required]
    public Guid AzureCredentialId { get; set; }
    
    [JsonIgnore]
    public AzureCredential? AzureCredential { get; [UsedImplicitly] init; }
    
    [JsonIgnore]
    public IList<FabricStep> FabricSteps { get; } = new List<FabricStep>();
    
    [JsonIgnore]
    public IList<DatasetStep> DatasetSteps { get; } = new List<DatasetStep>();
    
    [JsonIgnore]
    public IList<DataflowStep> DataflowSteps { get; } = new List<DataflowStep>();
}