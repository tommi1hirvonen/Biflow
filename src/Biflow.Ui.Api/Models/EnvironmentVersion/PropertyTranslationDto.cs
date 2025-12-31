namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record PropertyTranslationDto
{
    public required Guid PropertyTranslationSetId { get; init; }
    public required string PropertyTranslationName { get; init; }
    public required int Order { get; init; }
    public required string[] PropertyPaths { get; init; }
    public required string OldValue { get; init; }
    public required bool ExactMatch { get; init; }
    public required ParameterValue NewValue { get; init; }
}
