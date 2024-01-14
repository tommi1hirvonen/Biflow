using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class JobCategory
{
    [JsonInclude]
    public Guid CategoryId { get; private set; }

    [Required]
    [MaxLength(250)]
    [MinLength(1)]
    public string CategoryName { get; set; } = "";

    [JsonIgnore]
    public IEnumerable<Job> Jobs { get; } = new List<Job>();
}
