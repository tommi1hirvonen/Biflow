using Biflow.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class DatabricksWorkspace
{
    [JsonInclude]
    public Guid WorkspaceId { get; private set; }

    [MaxLength(250)]
    public string WorkspaceName { get; set; } = "";

    [Required]
    [MaxLength(4000)]
    public string WorkspaceUrl { get; set; } = "";

    [Required]
    [MaxLength(4000)]
    [JsonSensitive]
    public string ApiToken { get; set; } = "";

    [JsonIgnore]
    public IEnumerable<DatabricksStep> Steps { get; } = new List<DatabricksStep>();

    public DatabricksClientWrapper CreateClient() => new(this);
}
