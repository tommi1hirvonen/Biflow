namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record DatasetStepDto : StepDto
{
    public required Guid FabricWorkspaceId { get; init; }
    public required Guid DatasetId { get; init; }
    public string? DatasetName { get; init; }
}