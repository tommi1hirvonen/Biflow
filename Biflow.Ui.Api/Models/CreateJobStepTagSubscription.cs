namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record CreateJobStepTagSubscription
{
    public required Guid UserId { get; init; }
    public required Guid JobId { get; init; }
    public required Guid StepTagId { get; init; }
    public required AlertType AlertType { get; init; }
}