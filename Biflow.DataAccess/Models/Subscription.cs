using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class Subscription
{
    [Required]
    public Guid JobId { get; set; }

    public Job Job { get; set; } = null!;

    [Required]
    [ForeignKey("User")]
    public string? Username { get; set; }

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
