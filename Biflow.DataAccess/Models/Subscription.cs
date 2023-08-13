using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Subscription")]
[PrimaryKey("JobId", "Username")]
public class Subscription
{
    public Subscription(Guid jobId, string username)
    {
        JobId = jobId;
        Username = username;
    }

    [Required]
    public Guid JobId { get; }

    public Job Job { get; set; } = null!;

    [Required]
    [ForeignKey("User")]
    public string Username { get; }

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
