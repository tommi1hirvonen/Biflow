using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class StepTag(string tagName) : Tag(TagType.Step, tagName)
{
    [JsonIgnore]
    public ICollection<Step> Steps { get; } = new List<Step>();

    [JsonIgnore]
    public IEnumerable<JobStep> JobSteps { get; } = new List<JobStep>();

    [JsonIgnore]
    public IEnumerable<Schedule> Schedules { get; } = new List<Schedule>();

    [JsonIgnore]
    public IEnumerable<TagSubscription> TagSubscriptions { get; } = new List<TagSubscription>();

    [JsonIgnore]
    public IEnumerable<JobTagSubscription> JobTagSubscriptions { get; } = new List<JobTagSubscription>();
}
