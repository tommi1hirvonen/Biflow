namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record JobStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid JobToExecuteId { get; init; }
    public required bool ExecuteSynchronized { get; init; }
    public Guid[] FilterStepTagIds { get; init; } = [];
    public JobStepParameterDto[] Parameters { get; init; } = [];
}