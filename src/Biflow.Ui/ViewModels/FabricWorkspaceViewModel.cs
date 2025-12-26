using System.ComponentModel.DataAnnotations;
using Biflow.Core.Attributes.Validation;

namespace Biflow.Ui.ViewModels;

public class FabricWorkspaceViewModel
{
    public required Guid FabricWorkspaceId { get; init; }
    
    [Required, MaxLength(250)]
    public required string FabricWorkspaceName { get; set; }
    
    [NotEmptyGuid, Length(36, 36)]
    public required string WorkspaceId { get; set; }
    
    [Required]
    public required Guid AzureCredentialId { get; set; }
    
    public static FabricWorkspaceViewModel FromEntity(FabricWorkspace fabricWorkspace) => new()
    {
        FabricWorkspaceId = fabricWorkspace.FabricWorkspaceId,
        FabricWorkspaceName = fabricWorkspace.FabricWorkspaceName,
        WorkspaceId = fabricWorkspace.WorkspaceId.ToString(),
        AzureCredentialId = fabricWorkspace.AzureCredentialId
    };
}