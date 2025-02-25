namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record PinnedDto
{
    public required bool IsPinned { get; init; }
}