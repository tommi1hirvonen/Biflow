using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class TagSubscription(Guid userId, Guid tagId) : Subscription(userId, SubscriptionType.Tag)
{
    public AlertType AlertType { get; set; }

    [Required]
    public Guid TagId { get; init; } = tagId;

    [JsonIgnore]
    public StepTag Tag { get; init; } = null!;
}
