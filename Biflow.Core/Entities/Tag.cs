using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class Tag(string tagName) : ITag
{
    [JsonInclude]
    public Guid TagId { get; private set; }

    [Required]
    [MaxLength(250)]
    public string TagName { get; set; } = tagName;

    public TagColor Color { get; set; }

    [JsonIgnore]
    public IList<Step> Steps { get; set; } = null!;

    [JsonIgnore]
    public IList<JobStep> JobSteps { get; set; } = null!;

    [JsonIgnore]
    public IList<Schedule> Schedules { get; set; } = null!;

    [JsonIgnore]
    public IList<TagSubscription> TagSubscriptions { get; set; } = null!;

    [JsonIgnore]
    public IList<JobTagSubscription> JobTagSubscriptions { get; set; } = null!;
}
