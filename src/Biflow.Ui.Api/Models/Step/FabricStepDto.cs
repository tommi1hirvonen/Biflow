namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record FabricStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid FabricWorkspaceId { get; init; }
    public required FabricItemType ItemType { get; init; }
    public required Guid ItemId { get; init; }
    public required string ItemName { get; init; }
    public StepParameterDto[] Parameters { get; init; } = [];
}