namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record PipelineStepDto : StepDto
{
    public required int TimeoutMinutes { get; init; }
    public required Guid PipelineClientId { get; init; }
    public required string PipelineName { get; init; }
    public required StepParameterDto[] Parameters { get; init; }
}