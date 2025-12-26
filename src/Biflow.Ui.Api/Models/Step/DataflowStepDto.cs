namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record DataflowStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid FabricWorkspaceId { get; init; }
    public required Guid DataflowId { get; init; }
    public required string DataflowName { get; init; }
}