using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

[PublicAPI]
public class HealthReportEntryDto
{
    [JsonConstructor]
    public HealthReportEntryDto()
    {
    }

    public HealthReportEntryDto(HealthReportEntry entry)
    {
        Description = entry.Description;
        Status = entry.Status;
        Duration = entry.Duration;
        Error = entry.Exception?.Message;
        Tags = entry.Tags;
        Data = entry.Data.ToDictionary(x => x.Key, x => x.Value);
    }
    
    public string? Description { get; init; }
    
    public HealthStatus Status { get; init; }
    
    public TimeSpan Duration { get; init; }
    
    public string? Error { get; init; }

    public IEnumerable<string> Tags { get; init; } = [];
    
    public Dictionary<string, object> Data { get; init; } = [];
}