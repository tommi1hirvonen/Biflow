namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record HttpStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required string Url { get; init; }
    public required HttpStepMethod Method { get; init; } = HttpStepMethod.Post;
    public string? Body { get; init; }
    public HttpBodyFormat BodyFormat { get; init; } = HttpBodyFormat.PlainText;
    public HttpHeader[] Headers { get; init; } = [];
    public bool DisableAsyncPattern { get; init; }
    public StepParameterDto[] Parameters { get; init; } = [];
}