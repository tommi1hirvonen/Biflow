using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class JobSubscription(Guid userId, Guid jobId) : Subscription(userId, SubscriptionType.Job)
{

    /// <summary>
    /// null if regular subscription is not enabled
    /// </summary>
    public AlertType? AlertType { get; set; }

    [Required]
    public Guid JobId { get; set; } = jobId;

    public Job Job { get; set; } = null!;

    public bool NotifyOnOvertime { get; set; }
}
