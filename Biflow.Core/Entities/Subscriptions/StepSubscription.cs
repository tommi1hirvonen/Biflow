using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class StepSubscription(Guid userId, Guid stepId) : Subscription(userId, SubscriptionType.Step)
{
    public AlertType AlertType { get; set; }

    [Required]
    public Guid StepId { get; init; } = stepId;

    [JsonIgnore]
    public Step Step { get; init; } = null!;
}
