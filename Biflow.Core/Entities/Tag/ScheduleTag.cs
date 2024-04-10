using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class ScheduleTag(string tagName) : Tag(TagType.Schedule, tagName)
{
    [JsonIgnore]
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
