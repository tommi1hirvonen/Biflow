namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record CreateJobSubscription
{
    public required Guid UserId { get; init; }
    public required Guid JobId { get; init; }
    public required AlertType? AlertType { get; init; }
    public required bool NotifyOnOvertime { get; init; }
}