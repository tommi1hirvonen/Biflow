using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public record Tag
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
