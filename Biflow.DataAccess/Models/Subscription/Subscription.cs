using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Subscription")]
public abstract class Subscription
{
    public Subscription(Guid userId, SubscriptionType subscriptionType)
    {
        UserId = userId;
        SubscriptionType = subscriptionType;
    }

    [Key]
    public Guid SubscriptionId { get; private set; }

    public SubscriptionType SubscriptionType { get; }

    [Required]
    public Guid UserId { get; private set; }

    public User User { get; set; } = null!;
}