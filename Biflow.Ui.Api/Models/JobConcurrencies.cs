namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record JobConcurrencies
{
    public required int MaxParallelSteps { get; init; }
    public required JobConcurrencyDto[] StepTypeConcurrencies { get; init; }
}