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

    public async Task RefreshDatasetAsync(Guid workspaceId, string datasetId, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync();
        await client.Datasets.RefreshDatasetInGroupAsync(workspaceId, datasetId,
            cancellationToken: cancellationToken);
    }

    public async Task<(DatasetRefreshStatus? Status, Refresh? Refresh)> GetDatasetRefreshStatusAsync(
        Guid workspaceId,
        string datasetId,
        CancellationToken cancellationToken)
    {
        var client = await GetClientAsync();
        var refreshes = await client.Datasets.GetRefreshHistoryInGroupAsync(
            workspaceId,
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

    public async Task<IReadOnlyList<Dataset>> GetDatasetsAsync(Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync();
        var datasets = await client.Datasets.GetDatasetsInGroupAsync(workspaceId, cancellationToken);
        return datasets.Value
            .Select(d => new Dataset(workspaceId, d.Id, d.Name))
            .OrderBy(x => x.DatasetName)
            .ToArray();
    }

    public async Task<string> GetWorkspaceNameAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync();
        var filter = $"id eq '{workspaceId}'";
        var groups = await client.Groups.GetGroupsAsync(filter, top: 1, cancellationToken: cancellationToken);
        var group = groups.Value.First();
        return group.Name;
    }

    public async Task<string> GetDatasetNameAsync(Guid workspaceId, string datasetId,
        CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync();
        var dataset = await client.Datasets.GetDatasetInGroupAsync(workspaceId, datasetId, cancellationToken);
        return dataset.Name;
    }
}
