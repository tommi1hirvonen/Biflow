using System.ComponentModel.DataAnnotations;
using Azure.Core;
using Azure.Identity;
using Biflow.Core.Attributes;
using Biflow.Core.Interfaces;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;

namespace Biflow.Core.Entities;

public class OrganizationalAccountCredential() : AzureCredential(AzureCredentialType.OrganizationalAccount)
{
    [MaxLength(250)]
    [Required]
    public string Username { get; set; } = "";

    [MaxLength(250)]
    [Required]
    [JsonSensitive]
    public string Password { get; set; } = "";
    
    public override TokenCredential GetTokenCredential() =>
        new UsernamePasswordCredential(Username, Password, TenantId, ClientId);
    
    public override TokenCredential GetTokenServiceCredential(ITokenService tokenService) =>
        new UsernamePasswordAzureTokenCredential(tokenService, this);
    
    public override async Task TestConnection()
    {
        var credential = new UsernamePasswordCredential(
            Username, Password, TenantId, ClientId);
        var context = new TokenRequestContext([AzureResourceUrl]);
        _ = await credential.GetTokenAsync(context);
    }

    public override async Task TestPowerBiConnection()
    {
        var credential = new UsernamePasswordCredential(
            Username, Password, TenantId, ClientId);
        var context = new TokenRequestContext([PowerBiResourceUrl]);
        var token = await credential.GetTokenAsync(context);

        var credentials = new TokenCredentials(token.Token);
        var client = new PowerBIClient(credentials);
        _ = await client.Groups.GetGroupsAsync();
    }
}