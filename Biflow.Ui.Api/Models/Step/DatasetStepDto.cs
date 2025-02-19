namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record DatasetStepDto : StepDto
{
    public required Guid AzureCredentialId { get; init; }
    public required Guid WorkspaceId { get; init; }
    public string? WorkspaceName { get; init; }
    public required Guid DatasetId { get; init; }
    public string? DatasetName { get; init; }
}