using System.Data;

namespace Biflow.DataAccess;

public class EnvironmentSnapshotBuilder(IDbContextFactory<AppDbContext> dbContextFactory)
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<EnvironmentSnapshot> CreateAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        var connections = await context.Connections
            .OrderBy(c => c.ConnectionId)
            .ToArrayAsync();
        var credentials = await context.Credentials
            .OrderBy(c => c.Username)
            .ToArrayAsync();
        var appRegistrations = await context.AppRegistrations
            .OrderBy(a => a.AppRegistrationId)
            .ToArrayAsync();
        var pipelineClients = await context.PipelineClients
            .OrderBy(p => p.PipelineClientId)
            .ToArrayAsync();
        var functionApps = await context.FunctionApps
            .OrderBy(f => f.FunctionAppId)
            .ToArrayAsync();
        var qlikCloudClients = await context.QlikCloudEnvironments
            .OrderBy(q => q.QlikCloudEnvironmentId)
            .ToArrayAsync();
        var blobStorageClients = await context.BlobStorageClients
            .OrderBy(b => b.BlobStorageClientId)
            .ToArrayAsync();
        var databricksWorkspaces = await context.DatabricksWorkspaces
            .OrderBy(w => w.WorkspaceId)
            .ToArrayAsync();


        var jobs = await context.Jobs
            .Include(j => j.JobParameters.OrderBy(p => p.ParameterId))
            .Include(j => j.JobConcurrencies.OrderBy(c => c.StepType))
            .Include(j => j.Tags.OrderBy(t => t.TagId))
            .OrderBy(j => j.JobId)
            .ToArrayAsync();

        // Load steps and schedules separately in order to be able to sort their navigation collections.
        // Because change tracking on the DbContext is enabled, the steps and schedules navigation collections
        // on the jobs loaded previously will be automatically populated by EF Core.
        var steps = await context.Steps
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            .Include(s => (s as JobStep)!.TagFilters.OrderBy(t => t.TagId))
            .Include(s => s.Dependencies.OrderBy(d => d.DependantOnStepId))
            .Include(s => s.DataObjects.OrderBy(d => d.ObjectId))
            .Include(s => s.Tags.OrderBy(t => t.TagId))
            .Include(s => s.ExecutionConditionParameters.OrderBy(p => p.ParameterId))
            .OrderBy(s => s.JobId).ThenBy(s => s.StepId)
            .ToArrayAsync();
        var schedules = await context.Schedules
            .Include(s => s.TagFilter.OrderBy(t => t.TagId))
            .Include(s => s.Tags.OrderBy(t => t.TagId))
            .OrderBy(s => s.JobId).ThenBy(s => s.ScheduleId)
            .ToArrayAsync();

        var tags = await context.Tags
            .OrderBy(t => t.TagId)
            .ToArrayAsync();
        var dataObjects = await context.DataObjects
            .OrderBy(d => d.ObjectId)
            .ToArrayAsync();

        var dataTables = await context.MasterDataTables
            .Include(t => t.Lookups.OrderBy(l => l.LookupId))
            .OrderBy(t => t.DataTableId)
            .ToArrayAsync();
        var dataTableCategories = await context.MasterDataTableCategories
            .OrderBy(c => c.CategoryId)
            .ToArrayAsync();

        var snapshot = new EnvironmentSnapshot
        {
            Connections = connections,
            Credentials = credentials,
            AppRegistrations = appRegistrations,
            PipelineClients = pipelineClients,
            FunctionApps = functionApps,
            QlikCloudEnvironments = qlikCloudClients,
            DatabricksWorkspaces = databricksWorkspaces,
            BlobStorageClients = blobStorageClients,
            Jobs = jobs,
            Tags = tags,
            DataObjects = dataObjects,
            DataTables = dataTables,
            DataTableCategories = dataTableCategories
        };
        return snapshot;
    }
}
