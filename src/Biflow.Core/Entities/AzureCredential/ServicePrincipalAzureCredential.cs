using System.ComponentModel.DataAnnotations;
using Azure.Core;
using Azure.Identity;
using Biflow.Core.Attributes;

namespace Biflow.Core.Entities;

public class ServicePrincipalAzureCredential() : AzureCredential(AzureCredentialType.ServicePrincipal)
{
    [Required]
    [MaxLength(36)]
    [MinLength(36)]
    public string TenantId { get; set; } = "";

    [Required]
    [MaxLength(36)]
    [MinLength(36)]
    public string ClientId { get; set; } = "";
    
    [Required]
    [MaxLength(1000)]
    [JsonSensitive]
    public string ClientSecret { get; set; } = "";
    
    public override TokenCredential GetTokenCredential() =>
        new ClientSecretCredential(TenantId, ClientId, ClientSecret);
}