using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public abstract class Subscription(Guid userId, SubscriptionType subscriptionType)
{
    public Guid SubscriptionId { get; [UsedImplicitly] private set; }

    public SubscriptionType SubscriptionType { get; } = subscriptionType;

    [Required]
    public Guid UserId { get; private set; } = userId;

    [JsonIgnore]
    public User User { get; init; } = null!;
}