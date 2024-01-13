using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

public class StepSubscription(Guid userId, Guid stepId) : Subscription(userId, SubscriptionType.Step)
{
    [Column("AlertType")]
    public AlertType AlertType { get; set; }

    [Required]
    [Column("StepId")]
    public Guid StepId { get; set; } = stepId;

    public Step Step { get; set; } = null!;
}
