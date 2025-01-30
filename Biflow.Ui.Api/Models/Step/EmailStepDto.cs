namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record EmailStepDto : StepDto
{
    public required string[] Recipients { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required StepParameterDto[] Parameters { get; init; }
}