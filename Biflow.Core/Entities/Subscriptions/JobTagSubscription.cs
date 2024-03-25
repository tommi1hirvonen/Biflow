using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class JobTagSubscription(Guid userId, Guid jobId, Guid tagId) : Subscription(userId, SubscriptionType.JobTag)
{
    public AlertType AlertType { get; set; }

    [Required]
    public Guid JobId { get; set; } = jobId;

    [Required]
    public Guid TagId { get; set; } = tagId;

    public Job Job { get; set; } = null!;

    public StepTag Tag { get; set; } = null!;
}
