namespace Biflow.Ui.Api.Models;

public record UpdateJobSubscription
{
    public required AlertType? AlertType { get; init; }
    public required bool NotifyOnOvertime { get; init; }
}