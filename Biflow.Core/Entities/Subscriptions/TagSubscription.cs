using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class TagSubscription(Guid userId, Guid tagId) : Subscription(userId, SubscriptionType.Tag)
{
    public AlertType AlertType { get; set; }

    [Required]
    public Guid TagId { get; init; } = tagId;

    public StepTag Tag { get; init; } = null!;
}
