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
        var jobs = new List<DatabricksJob>();
        await foreach (var job in Client.Jobs.ListPageable(cancellationToken: cancellationToken))
        {
            jobs.Add(new(job.JobId, job.Settings.Name));
        }
        jobs.SortBy(j => j.JobName);
        return jobs;
    }

    public async Task<IEnumerable<Pipeline>> GetPipelinesAsync(CancellationToken cancellationToken = default)
    {
        var pipelines = new List<Pipeline>();
        await foreach (var pipeline in Client.Pipelines.ListPageable(cancellationToken: cancellationToken))
        {
            pipelines.Add(pipeline);
        }
        pipelines.SortBy(p => p.Name);
        return pipelines;
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

    public async Task<IEnumerable<ObjectInfo>> GetWorkspaceObjectsAsync(string path = "/", CancellationToken cancellationToken = default)
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

    public void Dispose() => Client.Dispose();
}
