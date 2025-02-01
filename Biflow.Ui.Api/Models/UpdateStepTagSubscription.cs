namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record UpdateStepTagSubscription
{
    public required AlertType AlertType { get; init; }
}