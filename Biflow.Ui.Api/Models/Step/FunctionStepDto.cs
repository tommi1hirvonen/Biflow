namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record FunctionStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid FunctionAppId { get; init; }
    public required string FunctionUrl { get; init; }
    public required string? FunctionInput { get; init; }
    public required bool FunctionIsDurable { get; init; }
    public required string? FunctionKey { get; init; }
    public required StepParameterDto[] Parameters { get; init; }
}