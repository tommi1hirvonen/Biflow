using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class JobTagSubscription(Guid userId, Guid jobId, Guid tagId) : Subscription(userId, SubscriptionType.JobTag)
{
    [Column("AlertType")]
    [MaxLength(20)]
    [Unicode(false)]
    public AlertType AlertType { get; set; }

    [Required]
    [Column("JobId")]
    public Guid JobId { get; set; } = jobId;

    [Required]
    [Column("TagId")]
    public Guid TagId { get; set; } = tagId;

    public Job Job { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}
