using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

[PublicAPI]
public class HealthReportDto
{
    [JsonConstructor]
    public HealthReportDto()
    {
    }

    public HealthReportDto(HealthReport report)
    {
        Status = report.Status;
        TotalDuration = report.TotalDuration;
        Entries = report.Entries.ToDictionary(
            e => e.Key,
            e => new HealthReportEntryDto(e.Value));
    }
    
    public HealthStatus Status { get; init; }
    
    public TimeSpan TotalDuration { get; init; }

    public Dictionary<string, HealthReportEntryDto> Entries { get; init; } = [];
}