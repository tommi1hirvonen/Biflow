using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class TagSubscription(Guid userId, Guid tagId) : Subscription(userId, SubscriptionType.Tag)
{
    public AlertType AlertType { get; set; }

    [Required]
    public Guid TagId { get; set; } = tagId;

    public Tag Tag { get; set; } = null!;
}
