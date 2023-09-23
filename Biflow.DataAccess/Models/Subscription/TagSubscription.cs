using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class TagSubscription : Subscription
{
    public TagSubscription(Guid userId, Guid tagId) : base(userId, SubscriptionType.Tag)
    {
        TagId = tagId;
    }

    [Column("AlertType")]
    public AlertType AlertType { get; set; }

    [Required]
    [Column("TagId")]
    public Guid TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}
