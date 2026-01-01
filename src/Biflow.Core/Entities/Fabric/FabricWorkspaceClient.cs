using System.Globalization;
using System.Text;
using Azure.Core;
using Biflow.Core.Interfaces;
using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.DataPipeline.Models;
using Microsoft.Fabric.Api.Notebook.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Biflow.Core.Entities;

public class FabricWorkspaceClient
{
    private const string Endpoint = "https://api.fabric.microsoft.com/v1";
    
    private readonly FabricClient _fabric;
    private readonly TokenCredential _credential;
    private readonly HttpClient _httpClient;
    
    public FabricWorkspaceClient(
        AzureCredential azureCredential,
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory)
    {
        var credential = azureCredential.GetTokenServiceCredential(tokenService);
        _credential = credential;
        _fabric = new FabricClient(credential);
        _httpClient = httpClientFactory.CreateClient();
    }
    
    public async Task<(bool Success, Guid InstanceId, string? ResponseContent)> StartOnDemandItemJobAsync(
        Guid workspaceId,
        Guid itemId,
        FabricItemType itemType,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var accessToken = await _credential.GetTokenAsync(
            new TokenRequestContext([AzureCredential.FabricResourceUrl]),
            cancellationToken);
        var (jobType, content) = itemType switch
        {
            FabricItemType.DataPipeline => ("Pipeline", CreatePipelineRequestContent(parameters)),
            FabricItemType.Notebook => ("RunNotebook", CreateNotebookRequestContent(parameters)),
            _ => throw new ArgumentOutOfRangeException($"Unrecognized item type: {itemType}")
        };
        var url = $"{Endpoint}/workspaces/{workspaceId}/items/{itemId}/jobs/{jobType}/instances";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken.Token}");
        request.Headers.Add("Accept", "application/json");
        request.Content = new StringContent(content, Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (false, Guid.Empty, responseContent);
        }
        if (response.Headers.Location is not { } uri)
        {
            throw new Exception("Location header was missing for on demand item job response");
        }
        var instanceId = ParseInstanceIdFromLocation(uri.AbsoluteUri.AsSpan());
        return (true, instanceId, responseContent);
    }

    private static string CreatePipelineRequestContent(IDictionary<string, object> parameters)
    {
        if (parameters.Count == 0)
            return "{}";
        var executionData = new { executionData = new { parameters } };
        return JsonSerializer.Serialize(executionData);
    }
    
    private static string CreateNotebookRequestContent(IDictionary<string, object> parameters)
    {
        // Notebooks require parameters to be serialized differently compared to pipelines.
        if (parameters.Count == 0)
            return "{}";
        var transformedParameters = parameters.ToDictionary(
            p => p.Key,
            p => new
            {
	            // Use ToString() formatting of ParameterValue for correct serialization
	            value = new ParameterValue(p.Value).ToString(),
	            // Allowed values for the type property are: int, float, bool, string
	            type = p.Value switch
	            {
		            short or int or long => "int",
		            float or double or decimal => "float",
		            bool => "bool",
		            _ => "string"
	            }
            });
        var executionData = new { executionData = new { parameters = transformedParameters } };
        return JsonSerializer.Serialize(executionData);
    }
    
    private static Guid ParseInstanceIdFromLocation(ReadOnlySpan<char> location)
    {
        // Use Spans to parse the instance id from the location url with zero allocation.
        var ranges = location.Split('/');
        Range? foundRange = null;
        // After iteration completes, foundRange will be last range found (=instance id).
        foreach (var r in ranges)
        {
            foundRange = r;
        }
        if (foundRange is not { } range)
        {
            throw new ArgumentException($"Could not parse instance id from {location}");    
        }
        var instanceId = location[range];
        return Guid.Parse(instanceId);
    }

    public async Task<ItemJobInstance> GetItemJobInstanceAsync(
        Guid workspaceId, Guid itemId, Guid instanceId, CancellationToken cancellationToken)
    {
        var response = await _fabric.Core.JobScheduler.GetItemJobInstanceAsync(
            workspaceId, itemId, instanceId, cancellationToken);
        return response.HasValue
            ? response.Value
            : throw new Exception($"Failed to get pipeline run for {instanceId}, response value is missing");
    }

    public Task CancelItemJobInstanceAsync(
        Guid workspaceId, Guid itemId, Guid instanceId, CancellationToken cancellationToken = default)
    {
        return _fabric.Core.JobScheduler.CancelItemJobInstanceAsync(
            workspaceId, itemId, instanceId, cancellationToken);
    }

    public async Task<string> GetWorkspaceNameAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var workspace = await _fabric.Core.Workspaces.GetWorkspaceAsync(workspaceId, cancellationToken);
        return workspace.HasValue
            ? workspace.Value.DisplayName
            : throw new Exception($"Workspace with id {workspaceId} not found");
    }

    public async Task<string> GetItemNameAsync(Guid workspaceId, Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var item = await _fabric.Core.Items.GetItemAsync(workspaceId, itemId, cancellationToken);
        return item.HasValue
            ? item.Value.DisplayName
            : throw new Exception($"Item with id {itemId} not found");
    }

    public async Task<IReadOnlyList<FabricItemGroup>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        // Load all workspaces in parallel.
        var workspaces = await _fabric.Core.Workspaces
            .ListWorkspacesAsync(cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        var tasks = workspaces
            .Select(async ws =>
            {
                var items = await GetItemsAsync(ws.Id, cancellationToken);
                var group = new FabricItemGroup(ws.Id, ws.DisplayName, items);
                return group;
            })
            .ToArray();
        var groups = await Task.WhenAll(tasks);
        groups.SortBy(g => g.WorkspaceName);
        return groups;
    }

    public async Task<IReadOnlyList<Item>> GetItemsAsync(Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        // Load pipelines and notebooks in parallel.
        var pipelinesTask = GetPipelinesAsync(workspaceId, cancellationToken);
        var notebooksTask = GetNotebooksAsync(workspaceId, cancellationToken);
        var pipelines = await pipelinesTask;
        var notebooks = await notebooksTask;
        var items = pipelines.Concat<Item>(notebooks).OrderBy(x => x.DisplayName).ToArray();
        return items;
    }

    public async Task<IReadOnlyList<Notebook>> GetNotebooksAsync(Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var notebooks = await _fabric.Notebook.Items
            .ListNotebooksAsync(workspaceId, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        return notebooks;
    }

    public async Task<IReadOnlyList<DataPipeline>> GetPipelinesAsync(Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var pipelines = await _fabric.DataPipeline.Items
            .ListDataPipelinesAsync(workspaceId, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        return pipelines;
    }
}