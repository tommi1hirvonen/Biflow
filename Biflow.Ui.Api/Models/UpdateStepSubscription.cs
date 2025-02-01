namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record UpdateStepSubscription
{
    public required AlertType AlertType { get; init; }
}