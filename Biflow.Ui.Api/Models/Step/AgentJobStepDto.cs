namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record AgentJobStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string AgentJobName { get; init; }
}