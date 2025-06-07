namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record TagDto
{
    public required string TagName { get; init; }
    public required TagColor Color { get; init; }
    public required int SortOrder { get; init; }
}