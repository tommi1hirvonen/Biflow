using Azure.Core;
using Azure.Identity;
using Biflow.Core.Attributes;
using Biflow.Core.Interfaces;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class AppRegistration
{
    [Required]
    [Display(Name = "App registration id")]
    [JsonInclude]
    public Guid AppRegistrationId { get; private set; }

    [Required]
    [MaxLength(250)]
    [Display(Name = "Power BI Service name")]
    public string? AppRegistrationName { get; set; }

    [Required]
    [Display(Name = "Tenant id")]
    [MaxLength(36)]
    [MinLength(36)]
    public string? TenantId { get; set; }

    [Required]
    [Display(Name = "Client id")]
    [MaxLength(36)]
    [MinLength(36)]
    public string? ClientId { get; set; }

    [Required]
    [Display(Name = "Client secret")]
    [MaxLength(1000)]
    [JsonSensitive]
    public string? ClientSecret { get; set; }

    [JsonIgnore]
    public IEnumerable<DatasetStep> Steps { get; } = new List<DatasetStep>();

    [JsonIgnore]
    public IEnumerable<PipelineClient> PipelineClients { get; } = new List<PipelineClient>();

    [JsonIgnore]
    public IEnumerable<FunctionApp> FunctionApps { get; } = new List<FunctionApp>();

    [JsonIgnore]
    public IEnumerable<BlobStorageClient> BlobStorageClients { get; } = new List<BlobStorageClient>();

    [JsonIgnore]
    public IEnumerable<AccessToken> AccessTokens { get; } = new List<AccessToken>();

    internal const string PowerBIResourceUrl = "https://analysis.windows.net/powerbi/api/.default";
    private const string AzureResourceUrl = "https://management.azure.com//.default";

    public DatasetClient CreateDatasetClient(ITokenService tokenService) => new(this, tokenService);

    public async Task TestConnection()
    {
        var credential = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        var context = new TokenRequestContext([AzureResourceUrl]);
        _ = await credential.GetTokenAsync(context);
    }

    public async Task TestPowerBIConnection()
    {
        var credential = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        var context = new TokenRequestContext([PowerBIResourceUrl]);
        var token = await credential.GetTokenAsync(context);

        var credentials = new TokenCredentials(token.Token);
        var client = new PowerBIClient(credentials);
        _ = await client.Groups.GetGroupsAsync();
    }
}