using System.ComponentModel.DataAnnotations;
using Azure.Core;
using Azure.Identity;
using Biflow.Core.Attributes;

namespace Biflow.Core.Entities;

public class OrganizationalAccountAzureCredential() : AzureCredential(AzureCredentialType.OrganizationalAccount)
{
    [Required]
    [MaxLength(36)]
    [MinLength(36)]
    public string TenantId { get; set; } = "";

    [Required]
    [MaxLength(36)]
    [MinLength(36)]
    public string ClientId { get; set; } = "";
    
    [MaxLength(250)]
    [Required]
    public string Username { get; set; } = "";

    [MaxLength(250)]
    [Required]
    [JsonSensitive]
    public string Password { get; set; } = "";
    
    public override TokenCredential GetTokenCredential() =>
        new UsernamePasswordCredential(Username, Password, TenantId, ClientId);
}