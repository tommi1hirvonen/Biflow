namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record DatabricksStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid DatabricksWorkspaceId { get; init; }
    public required DatabricksStepSettings Settings { get; init; }
    public StepParameterDto[] Parameters { get; init; } = [];
}