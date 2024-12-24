using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(JobSubscription), nameof(SubscriptionType.Job))]
[JsonDerivedType(typeof(JobTagSubscription), nameof(SubscriptionType.JobTag))]
[JsonDerivedType(typeof(StepSubscription), nameof(SubscriptionType.Step))]
[JsonDerivedType(typeof(TagSubscription), nameof(SubscriptionType.Tag))]
public abstract class Subscription(Guid userId, SubscriptionType subscriptionType)
{
    public Guid SubscriptionId { get; init; }

    public SubscriptionType SubscriptionType { get; } = subscriptionType;
    
    public Guid UserId { get; private set; } = userId;

    public User User { get; init; } = null!;
}