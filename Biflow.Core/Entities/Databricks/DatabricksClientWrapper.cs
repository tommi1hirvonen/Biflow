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
        return jobs;
    }

    public async Task<IEnumerable<ObjectInfo>> GetWorkspaceObjectsAsync(string path = "/", CancellationToken cancellationToken = default)
    {
        var objects = new List<ObjectInfo>();
        foreach (var info in await Client.Workspace.List(path, cancellationToken: cancellationToken))
        {
            objects.Add(info);
            if (info.ObjectType == ObjectType.DIRECTORY)
            {
                var subObjects = await GetWorkspaceObjectsAsync(info.Path, cancellationToken: cancellationToken);
                objects.AddRange(subObjects);
            }
        }
        return objects;
    }

    public void Dispose() => Client.Dispose();
}
