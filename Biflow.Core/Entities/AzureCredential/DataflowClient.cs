using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Biflow.Core.Interfaces;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;

namespace Biflow.Core.Entities;

public class DataflowClient(
    AzureCredential azureCredential, ITokenService tokenService, IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    
    private async Task<PowerBIClient> GetClientAsync()
    {
        var (accessToken, _) = await tokenService.GetTokenAsync(azureCredential, [AzureCredential.PowerBiResourceUrl]);
        var credentials = new TokenCredentials(accessToken);
        return new PowerBIClient(credentials);
    }

    public async Task RefreshDataflowAsync(string workspaceId, string dataflowId, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync();
        var refreshRequest = new RefreshRequest(NotifyOption.NoNotification);
        await client.Dataflows.RefreshDataflowAsync(
            Guid.Parse(workspaceId), Guid.Parse(dataflowId), refreshRequest, cancellationToken: cancellationToken);
    }

    public async Task<(DataflowRefreshStatus Status, DataflowTransaction Transaction)> GetDataflowTransactionStatusAsync(
        string workspaceId,
        string dataflowId,
        CancellationToken cancellationToken)
    {
        var client = await GetClientAsync();
        var transactions = await client.Dataflows.GetDataflowTransactionsAsync(
            Guid.Parse(workspaceId),
            Guid.Parse(dataflowId),
            cancellationToken);
        var transaction = transactions.Value.FirstOrDefault();
        var status = transaction?.Status switch
        {
            "InProgress" => DataflowRefreshStatus.InProgress,
            "Success" => DataflowRefreshStatus.Success,
            "Failed" => DataflowRefreshStatus.Failed,
            "Cancelled" => DataflowRefreshStatus.Cancelled,
            _ => throw new ApplicationException($"Unrecognized transaction status {transaction?.Status}")
        };
        return (status, transaction);
    }

    public async Task CancelDataflowRefreshAsync(
        string workspaceId, DataflowTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Call the dataflow transaction cancel endpoint manually, since the MS .NET library does not do it correctly.
        // It assumes the transaction id to be provided to the API is a Guid where in fact it's not.
        
        // NOTE: Requesting a dataflow transaction cancel currently does not work
        // with service principal or delegated authentication.
        var (accessToken, _) = await tokenService.GetTokenAsync(
            azureCredential, [AzureCredential.PowerBiResourceUrl], cancellationToken);
        var transactionId = HttpUtility.UrlEncode(transaction.Id);
        var url = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/dataflows/transactions/{transactionId}/cancel";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("Accept", "application/json");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<DataflowGroup>> GetAllDataflowsAsync()
    {
        var client = await GetClientAsync();
        var groups = await client.Groups.GetGroupsAsync();
        var dataflowGroups = new List<DataflowGroup>();
        foreach (var group in groups.Value)
        {
            var groupDataflows = await client.Dataflows.GetDataflowsAsync(group.Id);
            var dataflows = groupDataflows.Value
                .Select(f => new Dataflow(group.Id.ToString(), group.Name, f.ObjectId.ToString(), f.Name))
                .ToArray();
            var dataflowGroup = new DataflowGroup(group.Id.ToString(), group.Name, dataflows);
            dataflowGroups.Add(dataflowGroup);
        }
        return dataflowGroups;
    }

    public async Task<string> GetWorkspaceNameAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync();
        var filter = $"id eq '{workspaceId}'";
        var groups = await client.Groups.GetGroupsAsync(filter, top: 1, cancellationToken: cancellationToken);
        var group = groups.Value.First();
        return group.Name;
    }

    public async Task<string> GetDataflowNameAsync(
        string workspaceId, string dataflowId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync();
        await using var stream = await client.Dataflows.GetDataflowAsync(
            Guid.Parse(workspaceId), Guid.Parse(dataflowId), cancellationToken);
        var definition = JsonSerializer.Deserialize<DataflowDefinition>(stream);
        ArgumentNullException.ThrowIfNull(definition);
        return definition.Name;
    }
    
    private class DataflowDefinition
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; } 
    }
}