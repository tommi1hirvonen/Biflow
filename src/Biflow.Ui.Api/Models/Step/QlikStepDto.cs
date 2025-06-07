namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record QlikStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid QlikCloudEnvironmentId { get; init; }
    public required QlikStepSettings Settings { get; init; }
}