namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public sealed record SqlStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required string SqlStatement { get; init; }
    public required Guid ConnectionId { get; init; }
    public Guid? ResultCaptureJobParameterId { get; init; }
    public StepParameterDto[] Parameters { get; init; } = [];
}