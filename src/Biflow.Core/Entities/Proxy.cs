using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Biflow.Core.Attributes;

namespace Biflow.Core.Entities;

public class Proxy
{
    public Guid ProxyId { get; init; }

    [Required, MaxLength(250)]
    public string ProxyName { get; set; } = "";

    [Required, Url, MaxLength(500)]
    public string ProxyUrl { get; set; } = "";
    
    [MaxLength(500), JsonSensitive]
    public string? ApiKey { get; set; }
    
    [JsonIgnore]
    public IList<ExeStep> ExeSteps { get; } = new List<ExeStep>();
}