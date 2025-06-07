namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record DbtStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid DbtAccountId { get; init; }
    public required DbtJobDetails DbtJob { get; init; }
}