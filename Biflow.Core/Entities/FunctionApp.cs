using Biflow.Core.Attributes;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class FunctionApp
{
    [Display(Name = "Function app id")]
    public Guid FunctionAppId { get; init; }

    [Required]
    [Display(Name = "Function app name")]
    [MaxLength(250)]
    public string FunctionAppName { get; set; } = "";

    [Display(Name = "Function app key")]
    [MaxLength(1000)]
    [JsonSensitive]
    public string? FunctionAppKey
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? null : value;
    }

    [Required]
    [Display(Name = "Subscription id")]
    [MaxLength(36)]
    [MinLength(36)]
    public string SubscriptionId { get; set; } = "";

    [Required]
    [MaxLength(250)]
    [Display(Name = "Resource group name")]
    public string ResourceGroupName { get; set; } = "";

    [Required]
    [MaxLength(250)]
    [Display(Name = "Resource name")]
    public string ResourceName { get; set; } = "";

    [Required]
    public Guid AzureCredentialId { get; set; }

    [JsonIgnore]
    public AzureCredential AzureCredential
    {
        get;
        set
        {
            field = value;
            AzureCredentialId = value.AzureCredentialId;
        }
    } = null!;

    [Range(0, int.MaxValue)]
    public int MaxConcurrentFunctionSteps { get; set; }

    [JsonIgnore]
    public IEnumerable<FunctionStep> Steps { get; } = new List<FunctionStep>();

    public FunctionAppClient CreateClient(ITokenService tokenService, IHttpClientFactory httpClientFactory) =>
        new(this, tokenService, httpClientFactory);
}
