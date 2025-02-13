using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(JobSubscription), nameof(SubscriptionType.Job))]
[JsonDerivedType(typeof(JobStepTagSubscription), nameof(SubscriptionType.JobStepTag))]
[JsonDerivedType(typeof(StepSubscription), nameof(SubscriptionType.Step))]
[JsonDerivedType(typeof(StepTagSubscription), nameof(SubscriptionType.StepTag))]
public abstract class Subscription(Guid userId, SubscriptionType subscriptionType)
{
    public Guid SubscriptionId { get; init; }

    public SubscriptionType SubscriptionType { get; } = subscriptionType;
    
    public Guid UserId { get; private set; } = userId;

    public User User { get; init; } = null!;
}