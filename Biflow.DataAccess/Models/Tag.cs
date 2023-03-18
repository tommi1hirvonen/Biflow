using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Tag")]
public class Tag
{

    public Tag(string tagName)
    {
        TagName = tagName;
    }

    [Key]
    public Guid TagId { get; set; }

    [Required]
    [MaxLength(250)]
    [MinLength(1)]
    public string TagName { get; set; }

    public TagColor Color { get; set; }

    public IList<Step> Steps { get; set; } = null!;

    public IList<JobStep> JobSteps { get; set; } = null!;

    public IList<JobStepExecution> JobStepExecutions { get; set; } = null!;

    public IList<Schedule> Schedules { get; set; } = null!;
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
