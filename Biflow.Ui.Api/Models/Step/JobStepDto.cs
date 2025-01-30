namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record JobStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid JobToExecuteId { get; init; }
    public required bool ExecuteSynchronized { get; init; }
    public required Guid[] FilterStepTagIds { get; init; }
    public required JobStepParameterDto[] Parameters { get; init; }   
}