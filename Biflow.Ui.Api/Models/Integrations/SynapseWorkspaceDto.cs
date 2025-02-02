namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record SynapseWorkspaceDto
{
    public required string PipelineClientName { get; init; }
    public required int MaxConcurrentPipelineSteps { get; init; }
    public required Guid AzureCredentialId { get; init; }
    public required string SynapseWorkspaceUrl { get; init; }
}