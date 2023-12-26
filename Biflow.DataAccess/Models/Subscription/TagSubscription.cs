using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class TagSubscription(Guid userId, Guid tagId) : Subscription(userId, SubscriptionType.Tag)
{
    [Column("AlertType")]
    [MaxLength(20)]
    [Unicode(false)]
    public AlertType AlertType { get; set; }

    [Required]
    [Column("TagId")]
    public Guid TagId { get; set; } = tagId;

    public Tag Tag { get; set; } = null!;
}
