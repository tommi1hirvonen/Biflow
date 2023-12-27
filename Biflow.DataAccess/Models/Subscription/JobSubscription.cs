using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class JobSubscription(Guid userId, Guid jobId) : Subscription(userId, SubscriptionType.Job)
{

    /// <summary>
    /// null if regular subscription is not enabled
    /// </summary>
    [Column("AlertType")]
    public AlertType? AlertType { get; set; }

    [Required]
    [Column("JobId")]
    public Guid JobId { get; set; } = jobId;

    public Job Job { get; set; } = null!;

    public bool NotifyOnOvertime { get; set; }
}
