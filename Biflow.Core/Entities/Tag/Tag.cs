using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(StepTag), nameof(TagType.Step))]
[JsonDerivedType(typeof(JobTag), nameof(TagType.Job))]
[JsonDerivedType(typeof(ScheduleTag), nameof(TagType.Schedule))]
public abstract class Tag(TagType tagType, string tagName) : ITag
{
    [JsonInclude]
    public Guid TagId { get; private set; }

    [Required]
    [MaxLength(250)]
    public string TagName { get; set; } = tagName;

    public TagColor Color { get; set; }

    public TagType TagType { get; private set; } = tagType;
}
