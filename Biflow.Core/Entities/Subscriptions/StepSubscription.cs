using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class StepSubscription(Guid userId, Guid stepId) : Subscription(userId, SubscriptionType.Step)
{
    public AlertType AlertType { get; set; }

    [Required]
    public Guid StepId { get; set; } = stepId;

    public Step Step { get; set; } = null!;
}
