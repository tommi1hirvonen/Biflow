using Biflow.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class DbtAccount
{
    [JsonInclude]
    public Guid DbtAccountId { get; private set; }

    [MaxLength(250)]
    public string DbtAccountName { get; set; } = "";

    [Required]
    [MaxLength(4000)]
    public string ApiBaseUrl { get; set; } = "";

    [Required]
    [MaxLength(50)]
    public string AccountId { get; set; } = "";

    [Required]
    [MaxLength(4000)]
    [JsonSensitive]
    public string ApiToken { get; set; } = "";

    [JsonIgnore]
    public IEnumerable<DbtStep> Steps { get; } = new List<DbtStep>();

    public DbtClient CreateClient(IHttpClientFactory httpClientFactory) =>
        new(this, httpClientFactory);
}
