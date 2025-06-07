namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record PipelineStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid PipelineClientId { get; init; }
    public required string PipelineName { get; init; }
    public StepParameterDto[] Parameters { get; init; } = [];
}