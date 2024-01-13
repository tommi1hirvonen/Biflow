using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

public abstract class Subscription(Guid userId, SubscriptionType subscriptionType)
{
    public Guid SubscriptionId { get; private set; }

    public SubscriptionType SubscriptionType { get; } = subscriptionType;

    [Required]
    public Guid UserId { get; private set; } = userId;

    public User User { get; set; } = null!;
}