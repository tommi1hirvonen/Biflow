using System.Data;
using System.Text.Json;

namespace Biflow.DataAccess;

public class EnvironmentSnapshotBuilder(IDbContextFactory<AppDbContext> dbContextFactory)
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<string> CreateAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        var connections = await context.Connections
            .AsNoTracking()
            .OrderBy(c => c.ConnectionId)
            .ToArrayAsync();
        var credentials = await context.Credentials
            .AsNoTracking()
            .OrderBy(c => c.Username)
            .ToArrayAsync();
        var appRegistrations = await context.AppRegistrations
            .AsNoTracking()
            .OrderBy(a => a.AppRegistrationId)
            .ToArrayAsync();
        var pipelineClients = await context.PipelineClients
            .AsNoTracking()
            .OrderBy(p => p.PipelineClientId)
            .ToArrayAsync();
        var functionApps = await context.FunctionApps
            .AsNoTracking()
            .OrderBy(f => f.FunctionAppId)
            .ToArrayAsync();
        var qlikCloudClients = await context.QlikCloudClients
            .AsNoTracking()
            .OrderBy(q => q.QlikCloudClientId)
            .ToArrayAsync();
        var blobStorageClients = await context.BlobStorageClients
            .AsNoTracking()
            .OrderBy(b => b.BlobStorageClientId)
            .ToArrayAsync();


        var jobs = await context.Jobs
            .AsNoTracking()
            .Include(j => j.JobParameters.OrderBy(p => p.ParameterId))
            .Include(j => j.JobConcurrencies.OrderBy(c => c.StepType))
            .Include(j => j.Tags.OrderBy(t => t.TagId))
            .OrderBy(j => j.JobId)
            .ToArrayAsync();
        var steps = await context.Steps
            .AsNoTracking()
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
            .AsNoTracking()
            .Include(s => s.TagFilter.OrderBy(t => t.TagId))
            .Include(s => s.Tags.OrderBy(t => t.TagId))
            .OrderBy(s => s.JobId).ThenBy(s => s.ScheduleId)
            .ToArrayAsync();

        foreach (var job in jobs)
        {
            job.Steps.AddRange(steps.Where(s => s.JobId == job.JobId));
            job.Schedules.AddRange(schedules.Where(s => s.JobId == job.JobId));
        }

        var tags = await context.Tags
            .AsNoTracking()
            .OrderBy(t => t.TagId)
            .ToArrayAsync();
        var dataObjects = await context.DataObjects
            .AsNoTracking()
            .OrderBy(d => d.ObjectId)
            .ToArrayAsync();

        var dataTables = await context.MasterDataTables
            .AsNoTracking()
            .Include(t => t.Lookups.OrderBy(l => l.LookupId))
            .OrderBy(t => t.DataTableId)
            .ToArrayAsync();
        var dataTableCategories = await context.MasterDataTableCategories
            .AsNoTracking()
            .OrderBy(c => c.CategoryId)
            .ToArrayAsync();

        var snapshot = new EnvironmentSnapshot
        {
            Connections = connections,
            Credentials = credentials,
            AppRegistrations = appRegistrations,
            PipelineClients = pipelineClients,
            FunctionApps = functionApps,
            QlikCloudClients = qlikCloudClients,
            BlobStorageClients = blobStorageClients,
            Jobs = jobs,
            Tags = tags,
            DataObjects = dataObjects,
            DataTables = dataTables,
            DataTableCategories = dataTableCategories
        };
        var json = JsonSerializer.Serialize(snapshot, EnvironmentSnapshot.JsonSerializerOptions);
        ArgumentNullException.ThrowIfNull(json);
        return json;
    }
}
