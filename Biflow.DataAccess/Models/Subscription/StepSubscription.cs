using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class StepSubscription : Subscription
{
    public StepSubscription(Guid userId, Guid stepId) : base(userId, SubscriptionType.Step)
    {
        StepId = stepId;
    }

    [Column("AlertType")]
    public AlertType AlertType { get; set; }

    [Required]
    [Column("StepId")]
    public Guid StepId { get; set; }

    public Step Step { get; set; } = null!;
}
