namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public sealed record SqlStepDto : StepDto
{
    public required int TimeoutMinutes { get; init; }
    public required string SqlStatement { get; init; }
    public required Guid ConnectionId { get; init; }
    public required Guid? ResultCaptureJobParameterId { get; init; }
    public required StepParameterDto[] Parameters { get; init; }
}