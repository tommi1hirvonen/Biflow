namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record WaitStepDto : StepDto
{
    public required int WaitSeconds { get; init; }
}