using Biflow.Core.Interfaces;
using Microsoft.PowerBI.Api.Models;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;

namespace Biflow.Core.Entities;

public class DatasetClient(AzureCredential azureCredential, ITokenService tokenService)
{
    private async Task<PowerBIClient> GetClientAsync()
    {
        var (accessToken, _) = await tokenService.GetTokenAsync(azureCredential, [AzureCredential.PowerBiResourceUrl]);
        var credentials = new TokenCredentials(accessToken);
        return new PowerBIClient(credentials);
    }

    public async Task RefreshDatasetAsync(string groupId, string datasetId, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync();
        await client.Datasets.RefreshDatasetInGroupAsync(Guid.Parse(groupId), datasetId, cancellationToken: cancellationToken);
    }

    public async Task<(DatasetRefreshStatus? Status, Refresh? Refresh)> GetDatasetRefreshStatusAsync(
        string groupId,
        string datasetId,
        CancellationToken cancellationToken)
    {
        var client = await GetClientAsync();
        var refreshes = await client.Datasets.GetRefreshHistoryInGroupAsync(
            Guid.Parse(groupId),
            datasetId,
            top: 1,
            cancellationToken);
        var refresh = refreshes.Value.FirstOrDefault();
        var status = refresh?.Status switch
        {
            "Unknown" => DatasetRefreshStatus.Unknown,
            "Completed" => DatasetRefreshStatus.Completed,
            "Failed" => DatasetRefreshStatus.Failed,
            "Disabled" => DatasetRefreshStatus.Disabled,
            _ => throw new ApplicationException($"Unrecognized refresh status {refresh?.Status}")
        };
        return (status, refresh);
    }

    public async Task<IEnumerable<DatasetGroup>> GetAllDatasetsAsync()
    {
        var client = await GetClientAsync();
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

    public async Task<string> GetGroupNameAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync();
        var filter = $"id eq '{groupId}'";
        var groups = await client.Groups.GetGroupsAsync(filter, top: 1, cancellationToken: cancellationToken);
        var group = groups.Value.First();
        return group.Name;
    }

    public async Task<string> GetDatasetNameAsync(string groupId, string datasetId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync();
        var dataset = await client.Datasets.GetDatasetInGroupAsync(Guid.Parse(groupId), datasetId, cancellationToken);
        return dataset.Name;
    }
}
