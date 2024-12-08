using System.ComponentModel.DataAnnotations;
using Azure.Core;
using Azure.Identity;
using Biflow.Core.Attributes;
using Biflow.Core.Interfaces;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;

namespace Biflow.Core.Entities;

public class ServicePrincipalCredential() : AzureCredential(AzureCredentialType.ServicePrincipal)
{
    [Required]
    [MaxLength(1000)]
    [JsonSensitive]
    public string ClientSecret { get; set; } = "";
    
    public override TokenCredential GetTokenCredential() =>
        new ClientSecretCredential(TenantId, ClientId, ClientSecret);
    
    public override TokenCredential GetTokenServiceCredential(ITokenService tokenService) =>
        new ServicePrincipalAzureTokenCredential(tokenService, this);
    
    public override async Task TestConnection()
    {
        var credential = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        var context = new TokenRequestContext([AzureResourceUrl]);
        _ = await credential.GetTokenAsync(context);
    }

    public override async Task TestPowerBiConnection()
    {
        var credential = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        var context = new TokenRequestContext([PowerBiResourceUrl]);
        var token = await credential.GetTokenAsync(context);

        var credentials = new TokenCredentials(token.Token);
        var client = new PowerBIClient(credentials);
        _ = await client.Groups.GetGroupsAsync();
    }
}