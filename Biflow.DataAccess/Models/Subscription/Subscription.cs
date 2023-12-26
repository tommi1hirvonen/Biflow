using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Subscription")]
public abstract class Subscription(Guid userId, SubscriptionType subscriptionType)
{
    [Key]
    public Guid SubscriptionId { get; private set; }

    [MaxLength(20)]
    [Unicode(false)]
    public SubscriptionType SubscriptionType { get; } = subscriptionType;

    [Required]
    public Guid UserId { get; private set; } = userId;

    public User User { get; set; } = null!;
}