using Biflow.Core.Attributes;
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
    public Guid AppRegistrationId { get; set; }

    [JsonIgnore]
    public AppRegistration AppRegistration { get; set; } = null!;

    [JsonIgnore]
    public IList<FunctionStep> Steps { get; set; } = null!;
}
