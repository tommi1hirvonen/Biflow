namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record UpdateDatabricksWorkspace
{
    public required string WorkspaceName { get; init; }
    public required string WorkspaceUrl { get; init; }
    public string? ApiToken { get; init; }
}