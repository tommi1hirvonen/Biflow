namespace Biflow.DataAccess;

public class EnvironmentSnapshotBuilder(IDbContextFactory<AppDbContext> dbContextFactory)
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<EnvironmentSnapshot> CreateAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var sqlConnections = await context.SqlConnections
            .OrderBy(c => c.ConnectionId)
            .ToArrayAsync();
        var asConnections = await context.AnalysisServicesConnections
            .OrderBy(c => c.ConnectionId)
            .ToArrayAsync();
        var credentials = await context.Credentials
            .OrderBy(c => c.Username)
            .ToArrayAsync();
        var proxies = await context.Proxies
            .OrderBy(p => p.ProxyId)
            .ToArrayAsync();
        var azureCredentials = await context.AzureCredentials
            .OrderBy(a => a.AzureCredentialId)
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
        var dbtAccounts = await context.DbtAccounts
            .OrderBy(a => a.DbtAccountId)
            .ToArrayAsync();


        var jobs = await context.Jobs
            .Include(j => j.JobParameters.OrderBy(p => p.ParameterId))
            .Include(j => j.JobConcurrencies.OrderBy(c => c.StepType))
            .Include(j => j.Tags.OrderBy(t => t.TagId))
            .OrderBy(j => j.JobId)
            .ToArrayAsync();

        // Load steps and schedules separately in order to be able to sort their navigation collections.
        // Because change tracking on the DbContext is enabled, the steps and schedules navigation collections
        // on the jobs loaded previously will be automatically populated by EF Core
        // even if the query results here are discarded.
        _ = await context.Steps
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            .Include(s => (s as JobStep)!.TagFilters.OrderBy(t => t.TagId))
            .Include(s => s.Dependencies.OrderBy(d => d.DependantOnStepId))
            .Include(s => s.DataObjects.OrderBy(d => d.ObjectId))
            .Include(s => s.Tags.OrderBy(t => t.TagId))
            .Include(s => s.ExecutionConditionParameters.OrderBy(p => p.ParameterId))
            .OrderBy(s => s.JobId).ThenBy(s => s.StepId)
            .ToArrayAsync();
        _ = await context.Schedules
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
        var scdTables = await context.ScdTables
            .OrderBy(t => t.ScdTableId)
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
            SqlConnections = sqlConnections,
            AnalysisServicesConnections = asConnections,
            Credentials = credentials,
            Proxies = proxies,
            AzureCredentials = azureCredentials,
            PipelineClients = pipelineClients,
            FunctionApps = functionApps,
            QlikCloudEnvironments = qlikCloudClients,
            DatabricksWorkspaces = databricksWorkspaces,
            DbtAccounts = dbtAccounts,
            BlobStorageClients = blobStorageClients,
            Jobs = jobs,
            Tags = tags,
            DataObjects = dataObjects,
            ScdTables = scdTables,
            DataTables = dataTables,
            DataTableCategories = dataTableCategories
        };
        return snapshot;
    }
}
