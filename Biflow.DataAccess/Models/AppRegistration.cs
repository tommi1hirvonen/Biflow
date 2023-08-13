using Azure.Core;
using Azure.Identity;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("AppRegistration")]
public class AppRegistration
{
    [Key]
    [Required]
    [Display(Name = "App registration id")]
    public Guid AppRegistrationId { get; private set; }

    [Required]
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
    public string? ClientSecret { get; set; }

    public IList<DatasetStep> Steps { get; set; } = null!;

    public IList<PipelineClient> PipelineClients { get; set; } = null!;

    public IList<FunctionApp> FunctionApps { get; set; } = null!;

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

    public async Task<Dictionary<(string GroupId, string GroupName), List<(string DatasetId, string DatasetName)>>> GetAllDatasetsAsync(ITokenService tokenService)
    {
        var client = await GetClientAsync(tokenService);
        var groups = await client.Groups.GetGroupsAsync();
        var datasets = new Dictionary<(string GroupId, string GroupName), List<(string DatasetId, string DatasetName)>>();
        foreach (var group in groups.Value)
        {
            var groupDatasets = await client.Datasets.GetDatasetsInGroupAsync(group.Id);
            var key = (group.Id.ToString(), group.Name);
            datasets[key] = groupDatasets.Value.Select(d => (d.Id.ToString(), d.Name)).ToList();
        }
        return datasets;
    }

    public async Task TestConnection()
    {
        var credential = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        var context = new TokenRequestContext(new[] { AzureResourceUrl });
        var _ = await credential.GetTokenAsync(context);
    }

    public async Task TestPowerBIConnection()
    {
        var credential = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        var context = new TokenRequestContext(new[] { PowerBIResourceUrl });
        var token = await credential.GetTokenAsync(context);

        var credentials = new TokenCredentials(token.Token);
        var client = new PowerBIClient(credentials);
        var _ = await client.Groups.GetGroupsAsync();
    }
}
