namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record UpdateJobSubscription
{
    public required AlertType? AlertType { get; init; }
    public required bool NotifyOnOvertime { get; init; }
}