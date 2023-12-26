using Azure.Core;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("AppRegistration")]
public class AppRegistration
{
    [Key]
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
    [Unicode(false)]
    public string? TenantId { get; set; }

    [Required]
    [Display(Name = "Client id")]
    [MaxLength(36)]
    [MinLength(36)]
    [Unicode(false)]
    public string? ClientId { get; set; }

    [Required]
    [Display(Name = "Client secret")]
    [MaxLength(1000)]
    [Unicode(false)]
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

    private const string PowerBIResourceUrl = "https://analysis.windows.net/powerbi/api/.default";
    private const string AzureResourceUrl = "https://management.azure.com//.default";

    private async Task<PowerBIClient> GetClientAsync(ITokenService tokenService)
    {
        var (accessToken, _) = await tokenService.GetTokenAsync(this, PowerBIResourceUrl);
        var credentials = new TokenCredentials(accessToken);
        return new PowerBIClient(credentials);
    }

    public async Task RefreshDatasetAsync(ITokenService tokenService, string groupId, string datasetId, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(tokenService);
        await client.Datasets.RefreshDatasetInGroupAsync(Guid.Parse(groupId), datasetId, cancellationToken: cancellationToken);
    }

    public async Task<Refresh?> GetDatasetRefreshStatus(ITokenService tokenService, string groupId, string datasetId, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(tokenService);
        var refresh = await client.Datasets.GetRefreshHistoryInGroupAsync(Guid.Parse(groupId), datasetId, top: 1, cancellationToken);
        return refresh.Value.FirstOrDefault();
    }

    public async Task<IEnumerable<DatasetGroup>> GetAllDatasetsAsync(ITokenService tokenService)
    {
        var client = await GetClientAsync(tokenService);
        var groups = await client.Groups.GetGroupsAsync();
        var datasetGroups = new List<DatasetGroup>();
        foreach (var group in groups.Value)
        {
            var groupDatasets = await client.Datasets.GetDatasetsInGroupAsync(group.Id);
            var datasets = groupDatasets.Value
                .Select(d => new Dataset(group.Id.ToString(), group.Name, d.Id.ToString(), d.Name))
                .ToArray();
            var datasetGroup = new DatasetGroup(group.Id.ToString(), group.Name, datasets);
            datasetGroups.Add(datasetGroup);
        }
        return datasetGroups;
    }

    public async Task<string> GetGroupNameAsync(string groupId, ITokenService tokenService, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(tokenService);
        var filter = $"id eq '{groupId}'";
        var groups = await client.Groups.GetGroupsAsync(filter, top: 1, cancellationToken: cancellationToken);
        var group = groups.Value.First();
        return group.Name;
    }

    public async Task<string> GetDatasetNameAsync(string groupId, string datasetId, ITokenService tokenService, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(tokenService);
        var dataset = await client.Datasets.GetDatasetInGroupAsync(Guid.Parse(groupId), datasetId, cancellationToken);
        return dataset.Name;
    }

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
