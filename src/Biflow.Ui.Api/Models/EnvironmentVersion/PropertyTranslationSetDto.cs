namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record PropertyTranslationSetDto
{
    public required string PropertyTranslationSetName { get; init; }
}
