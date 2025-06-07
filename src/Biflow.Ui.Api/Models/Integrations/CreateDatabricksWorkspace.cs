namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record CreateDatabricksWorkspace
{
    public required string WorkspaceName { get; init; }
    public required string WorkspaceUrl { get; init; }
    public required string ApiToken { get; init; }
}