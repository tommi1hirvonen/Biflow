using Biflow.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class QlikCloudEnvironment
{
    public Guid QlikCloudEnvironmentId { get; init; }

    [Required]
    [MaxLength(250)]
    public required string QlikCloudEnvironmentName { get; set; }

    [Required]
    [MaxLength(4000)]
    public required string EnvironmentUrl { get; set; }

    [Required]
    [MaxLength(4000)]
    [JsonSensitive]
    public required string ApiToken { get; set; }

    [JsonIgnore]
    public IEnumerable<QlikStep> Steps { get; } = new List<QlikStep>();

    public QlikCloudClient CreateClient(IHttpClientFactory httpClientFactory) =>
        new(this, httpClientFactory);
}