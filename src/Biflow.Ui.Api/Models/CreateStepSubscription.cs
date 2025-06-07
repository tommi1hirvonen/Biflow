namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record CreateStepSubscription
{
    public required Guid UserId { get; init; }
    public required Guid StepId { get; init; }
    public required AlertType AlertType { get; init; }
}