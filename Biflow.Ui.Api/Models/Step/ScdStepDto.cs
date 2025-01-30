namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record ScdStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ScdTableId { get; init; }
}