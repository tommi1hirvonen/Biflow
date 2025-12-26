namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record FabricWorkspaceDto
{
    public required string FabricWorkspaceName { get; init; }
    public required Guid WorkspaceId { get; init; }
    public required Guid AzureCredentialId { get; init; }
}