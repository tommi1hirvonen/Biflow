using Biflow.Core.Attributes;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class FunctionApp
{
    [Display(Name = "Function app id")]
    [JsonInclude]
    public Guid FunctionAppId { get; private set; }

    [Required]
    [Display(Name = "Function app name")]
    [MaxLength(250)]
    public string FunctionAppName { get; set; } = "";

    [Display(Name = "Function app key")]
    [MaxLength(1000)]
    [JsonSensitive]
    public string? FunctionAppKey
    {
        get => _functionAppKey;
        set => _functionAppKey = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _functionAppKey;

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
    [Display(Name = "App registration")]
    public Guid AppRegistrationId { get; private set; }

    [JsonIgnore]
    public AppRegistration AppRegistration
    {
        get => _appRegistration;
        set
        {
            _appRegistration = value;
            AppRegistrationId = value.AppRegistrationId;
        }
    }

    private AppRegistration _appRegistration = null!;

    [JsonIgnore]
    public IEnumerable<FunctionStep> Steps { get; } = new List<FunctionStep>();

    public FunctionAppClient CreateClient(ITokenService tokenService, IHttpClientFactory httpClientFactory) =>
        new(this, tokenService, httpClientFactory);
}
