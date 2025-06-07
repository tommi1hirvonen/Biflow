namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record UpdateJobStepTagSubscription
{
    public required AlertType AlertType { get; init; }
}