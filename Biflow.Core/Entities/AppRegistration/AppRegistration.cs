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
    public IList<DatasetStep> Steps { get; set; } = null!;

    [JsonIgnore]
    public IList<PipelineClient> PipelineClients { get; set; } = null!;

    [JsonIgnore]
    public IList<FunctionApp> FunctionApps { get; set; } = null!;

    [JsonIgnore]
    public IList<BlobStorageClient> BlobStorageClients { get; set; } = null!;

    [JsonIgnore]
    public IList<AccessToken> AccessTokens { get; set; } = null!;

    internal const string PowerBIResourceUrl = "https://analysis.windows.net/powerbi/api/.default";
    private const string AzureResourceUrl = "https://management.azure.com//.default";

    public DatasetClient CreateDatasetClient(ITokenService tokenService) => new(this, tokenService);

    public async Task TestConnection()
    {
        var credential = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        var context = new TokenRequestContext([AzureResourceUrl]);
        var _ = await credential.GetTokenAsync(context);
    }

    public async Task TestPowerBIConnection()
    {
        var credential = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        var context = new TokenRequestContext([PowerBIResourceUrl]);
        var token = await credential.GetTokenAsync(context);

        var credentials = new TokenCredentials(token.Token);
        var client = new PowerBIClient(credentials);
        var _ = await client.Groups.GetGroupsAsync();
    }
}