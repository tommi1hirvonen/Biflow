using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class JobTagSubscription : Subscription
{
    public JobTagSubscription(Guid userId, Guid jobId, Guid tagId) 
        : base(userId, SubscriptionType.JobTag)
    {
        JobId = jobId;
        TagId = tagId;
    }

    [Column("AlertType")]
    public AlertType AlertType { get; set; }

    [Required]
    [Column("JobId")]
    public Guid JobId { get; set; }

    [Required]
    [Column("TagId")]
    public Guid TagId { get; set; }

    public Job Job { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}
