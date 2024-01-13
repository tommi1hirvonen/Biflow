using Biflow.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class QlikCloudClient
{
    [JsonInclude]
    public Guid QlikCloudClientId { get; private set; }

    [Required]
    [MaxLength(250)]
    public required string QlikCloudClientName { get; set; }

    [Required]
    [MaxLength(4000)]
    public required string EnvironmentUrl { get; set; }

    [Required]
    [MaxLength(4000)]
    [JsonSensitive]
    public required string ApiToken { get; set; }

    [JsonIgnore]
    public ICollection<QlikStep> Steps { get; set; } = null!;

    public QlikCloudConnectedClient CreateConnectedClient(IHttpClientFactory httpClientFactory) =>
        new(this, httpClientFactory);
}