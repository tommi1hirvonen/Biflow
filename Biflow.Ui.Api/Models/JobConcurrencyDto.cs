namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record JobConcurrencyDto(StepType StepType, int MaxParallelSteps);