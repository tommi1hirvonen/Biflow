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
    public IEnumerable<StepTagSubscription> StepTagSubscriptions { get; } = new List<StepTagSubscription>();

    [JsonIgnore]
    public IEnumerable<JobStepTagSubscription> JobStepTagSubscriptions { get; } = new List<JobStepTagSubscription>();
}
