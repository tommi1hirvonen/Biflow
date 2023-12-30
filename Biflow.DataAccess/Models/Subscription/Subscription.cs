using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Subscription")]
public abstract class Subscription(Guid userId, SubscriptionType subscriptionType)
{
    [Key]
    public Guid SubscriptionId { get; private set; }

    public SubscriptionType SubscriptionType { get; } = subscriptionType;

    [Required]
    public Guid UserId { get; private set; } = userId;

    public User User { get; set; } = null!;
}