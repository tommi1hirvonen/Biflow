namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record StateDto
{
    public required bool IsEnabled { get; init; }
}