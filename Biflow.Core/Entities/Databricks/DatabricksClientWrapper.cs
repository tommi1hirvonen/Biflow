using Microsoft.Azure.Databricks.Client;
using Microsoft.Azure.Databricks.Client.Models;

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
        var objects = new List<ObjectInfo>();
        foreach (var info in await Client.Workspace.List(path, cancellationToken: cancellationToken))
        {
            objects.Add(info);
            if (info.ObjectType == ObjectType.DIRECTORY)
            {
                // Get subdirectory contents recursively.
                var subObjects = await GetWorkspaceObjectsAsync(info.Path, cancellationToken: cancellationToken);
                objects.AddRange(subObjects);
            }
        }
        return objects;
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
