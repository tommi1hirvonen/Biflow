using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class JobSubscription : Subscription
{
    public JobSubscription(Guid userId, Guid jobId)
        : base(userId, SubscriptionType.Job)
    {
        JobId = jobId;
    }

    /// <summary>
    /// null if regular subscription is not enabled
    /// </summary>
    [Column("AlertType")]
    public AlertType? AlertType { get; set; }

    [Required]
    [Column("JobId")]
    public Guid JobId { get; set; }

    public Job Job { get; set; } = null!;

    public bool NotifyOnOvertime { get; set; }
}
