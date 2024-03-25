using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class JobTag(string tagName) : Tag(TagType.Job, tagName)
{
    [JsonIgnore]
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
