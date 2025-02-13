namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record FunctionStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid FunctionAppId { get; init; }
    public required string FunctionUrl { get; init; }
    public required string? FunctionInput { get; init; }
    public FunctionInputFormat FunctionInputFormat { get; init; } = FunctionInputFormat.PlainText;
    public required bool FunctionIsDurable { get; init; }
    public string? FunctionKey { get; init; }
    public StepParameterDto[] Parameters { get; init; } = [];
}