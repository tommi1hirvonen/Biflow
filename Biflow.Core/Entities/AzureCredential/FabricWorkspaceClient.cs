using Biflow.Core.Interfaces;
using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;

namespace Biflow.Core.Entities;

public class FabricWorkspaceClient
{
    private readonly FabricClient _fabric;
    
    public FabricWorkspaceClient(AzureCredential azureCredential, ITokenService tokenService)
    {
        var credential = azureCredential.GetTokenServiceCredential(tokenService);
        _fabric = new(credential);
    }
    
    public async Task<Guid> StartOnDemandItemJobAsync(
        Guid workspaceId,
        Guid itemId,
        FabricItemType itemType,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var request = parameters.Count > 0
            ? new RunOnDemandItemJobRequest { ExecutionData = new { parameters } }
            : null;
        var jobType = itemType switch
        {
            FabricItemType.DataPipeline => "Pipeline",
            FabricItemType.Notebook => "RunNotebook",
            _ => throw new ArgumentOutOfRangeException($"Unrecognized item type: {itemType}")
        };
        var response = await _fabric.Core.JobScheduler.RunOnDemandItemJobAsync(
            workspaceId, itemId, jobType, request, cancellationToken);
        if (!response.Headers.TryGetValue("Location", out var location))
        {
            throw new Exception("Location header was missing for on demand item job response");
        }
        var locationComponents = location.Split('/');
        var instanceId = Guid.Parse(locationComponents.Last());
        return instanceId;
    }

    public async Task<ItemJobInstance> GetItemJobInstanceAsync(
        Guid workspaceId, Guid itemId, Guid instanceId, CancellationToken cancellationToken)
    {
        var response = await _fabric.Core.JobScheduler.GetItemJobInstanceAsync(
            workspaceId, itemId, instanceId, cancellationToken);
        if (!response.HasValue)
        {
            throw new Exception($"Failed to get pipeline run for {instanceId}, response value is missing");
        }
        return response.Value;
    }

    public Task CancelItemJobInstanceAsync(
        Guid workspaceId, Guid itemId, Guid instanceId, CancellationToken cancellationToken = default)
    {
        return _fabric.Core.JobScheduler.CancelItemJobInstanceAsync(
            workspaceId, itemId, instanceId, cancellationToken);
    }

    public async Task<IReadOnlyList<FabricItemGroup>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        // Load all workspaces in parallel.
        var workspaces = await _fabric.Core.Workspaces
            .ListWorkspacesAsync(cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        var tasks = workspaces
            .Select(ws => GetItemsAsync(ws.Id, ws.DisplayName, cancellationToken))
            .ToArray();
        var groups = await Task.WhenAll(tasks);
        groups.SortBy(g => g.WorkspaceName);
        return groups;
    }

    private async Task<FabricItemGroup> GetItemsAsync(
        Guid workspaceId, string workspaceName, CancellationToken cancellationToken)
    {
        // Load pipelines and notebooks in parallel.
        var pipelinesTask = _fabric.DataPipeline.Items
            .ListDataPipelinesAsync(workspaceId, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        var notebooksTask = _fabric.Notebook.Items
            .ListNotebooksAsync(workspaceId, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        var pipelines = await pipelinesTask;
        var notebooks = await notebooksTask;
        var items = pipelines.Concat<Item>(notebooks).OrderBy(x => x.DisplayName).ToArray();
        var group = new FabricItemGroup(workspaceId, workspaceName, items);
        return group;
    }
}