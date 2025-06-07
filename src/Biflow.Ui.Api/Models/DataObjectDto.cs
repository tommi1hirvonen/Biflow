namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record DataObjectDto
{
    public required string ObjectUri { get; init; }
    public required int MaxConcurrentWrites { get; init; }
}