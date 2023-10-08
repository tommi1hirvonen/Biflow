using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Tag")]
public class Tag(string tagName) : ITag
{
    [Key]
    public Guid TagId { get; private set; }

    [Required]
    [MaxLength(250)]
    [MinLength(1)]
    public string TagName { get; set; } = tagName;

    public TagColor Color { get; set; }

    public IList<Step> Steps { get; set; } = null!;

    public IList<JobStep> JobSteps { get; set; } = null!;

    public IList<Schedule> Schedules { get; set; } = null!;

    public IList<TagSubscription> TagSubscriptions { get; set; } = null!;

    public IList<JobTagSubscription> JobTagSubscriptions { get; set; } = null!;
}

public enum TagColor
{
    LightGray,
    DarkGray,
    Purple,
    Green,
    Blue,
    Yellow,
    Red
}
