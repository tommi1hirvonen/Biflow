using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Subscription")]
[PrimaryKey("JobId", "UserId")]
public class Subscription
{
    public Subscription(Guid jobId, Guid userId)
    {
        JobId = jobId;
        UserId = userId;
    }

    [Required]
    public Guid JobId { get; }

    public Job Job { get; set; } = null!;

    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; }

    /// <summary>
    /// null if regular subscription is not enabled
    /// </summary>
    public SubscriptionType? SubscriptionType { get; set; }

    public bool NotifyOnOvertime { get; set; }

    public User User { get; set; } = null!;
}

public enum SubscriptionType
{
    OnFailure,
    OnSuccess,
    OnCompletion
}
