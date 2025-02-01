namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record JobConcurrencyDto
{
    public required StepType StepType { get; init; }
    public required int MaxParallelSteps { get; init; }
};