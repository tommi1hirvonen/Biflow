using Microsoft.Azure.Databricks.Client;
using Microsoft.Azure.Databricks.Client.Models;
using System.Collections.Concurrent;

namespace Biflow.Core.Entities;

public class DatabricksClientWrapper(DatabricksWorkspace workspace) : IDisposable
{
    public DatabricksClient Client { get; } =
        DatabricksClient.CreateClient(workspace.WorkspaceUrl, workspace.ApiToken);

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        _ = await Client.Workspace.List("/", cancellationToken);
    }
   
    public async Task<IEnumerable<DatabricksJob>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await Client.Jobs
            .ListPageable(cancellationToken: cancellationToken)
            .Select(job => new DatabricksJob(job.JobId, job.Settings.Name))
            .ToListAsync(cancellationToken);
        jobs.SortBy(j => j.JobName);
        return jobs;
    }

    public async Task<DatabricksJob?> GetJobAsync(long jobId, CancellationToken cancellationToken = default)
    {
        var job = await Client.Jobs.Get(jobId, cancellationToken);
        return job is not null
            ? new(job.JobId, job.Settings.Name)
            : null;
    }

    public async Task<IEnumerable<Pipeline>> GetPipelinesAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<Pipeline>();

        // Manually paginate the results since using Client.Pipelines.ListPageable()
        // throws when there are no pipelines in the workspace.
        PipelinesList? pipelines = null;
        do
        {
            pipelines = await Client.Pipelines.List(
                maxResults: 100,
                pageToken: pipelines?.NextPageToken,
                cancellationToken: cancellationToken);
            // pipelines.Pipelines can be null if there are no pipelines in the workspace.
            // The NuGet library doesn't have nullable enabled so there are no warnings of dereferencing null.
            result.AddRange(pipelines.Pipelines ?? []);
        } while (!string.IsNullOrEmpty(pipelines.NextPageToken));
        result.SortBy(p => p.Name);
        return result;
    }

    public Task<Pipeline?> GetPipelineAsync(string pipelineId, CancellationToken cancellationToken = default)
    {
        return Client.Pipelines.Get(pipelineId, cancellationToken);
    }

    public async Task<DatabricksFolder> GetWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        var objects = await GetWorkspaceObjectsAsync(cancellationToken: cancellationToken);
        var files = objects
            .Where(o => o.ObjectType != ObjectType.DIRECTORY)
            .Select(o =>
            {
                // The file paths are in format "/Users/john.doe@mydomain.com/Folder/file.py"
                var pathComponents = o.Path.Split('/');
                var folderComponents = pathComponents.Skip(1).SkipLast(1);
                var folder = string.Join('/', folderComponents);
                var file = new DatabricksFile(o.Path, pathComponents.Last(), folder);
                return file;
            });
        var folder = DatabricksFolder.FromFiles(files);
        return folder;
    }

    private async Task<IEnumerable<ObjectInfo>> GetWorkspaceObjectsAsync(string path = "/",
	    CancellationToken cancellationToken = default)
    {
        var result = new ConcurrentBag<ObjectInfo>();
        var items = await Client.Workspace.List(path, cancellationToken: cancellationToken);
        foreach (var item in items)
        {
            result.Add(item);
        }
        // Concurrently and recursively fetch subfolder items.
        await Parallel.ForEachAsync(
            items.Where(i => i.ObjectType == ObjectType.DIRECTORY),
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 3 },
            async (item, token) =>
            {
                var subItems = await GetWorkspaceObjectsAsync(item.Path, cancellationToken: token);
                foreach (var subItem in subItems)
                {
                    result.Add(subItem);
                }
            });
        return result.ToArray();
    }

    public Task<IDictionary<string, string>> GetRuntimeVersionsAsync(CancellationToken cancellationToken = default)
    {
        return Client.Clusters.ListSparkVersions(cancellationToken);
    }

    public Task<IEnumerable<NodeType>> GetNodeTypesAsync(CancellationToken cancellationToken = default)
    {
        return Client.Clusters.ListNodeTypes(cancellationToken);
    }

    public Task<IEnumerable<ClusterInfo>> GetClustersAsync(CancellationToken cancellationToken = default)
    {
        return Client.Clusters.List(cancellationToken);
    }

    public Task<ClusterInfo?> GetClusterAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        return Client.Clusters.Get(clusterId, cancellationToken);
    }

    public Task<IEnumerable<WarehouseInfo>> GetWarehousesAsync(CancellationToken cancellationToken = default)
    {
	    return Client.SQL.Warehouse.List(runAsUserId: null, cancellationToken: cancellationToken);
    }

    public Task<WarehouseInfo?> GetWarehouseAsync(string warehouseId, CancellationToken cancellationToken = default)
    {
	    return Client.SQL.Warehouse.Get(warehouseId, cancellationToken);
    }

    public void Dispose() => Client.Dispose();
}
