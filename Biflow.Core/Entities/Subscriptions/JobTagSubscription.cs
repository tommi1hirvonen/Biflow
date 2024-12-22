using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class JobTagSubscription(Guid userId, Guid jobId, Guid tagId) : Subscription(userId, SubscriptionType.JobTag)
{
    public AlertType AlertType { get; set; }

    [Required]
    public Guid JobId { get; init; } = jobId;

    [Required]
    public Guid TagId { get; init; } = tagId;

    [JsonIgnore]
    public Job Job { get; init; } = null!;

    [JsonIgnore]
    public StepTag Tag { get; init; } = null!;
}
