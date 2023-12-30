using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
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


        var jobCategories = await context.JobCategories
            .AsNoTracking()
            .OrderBy(c => c.CategoryId)
            .ToArrayAsync();

        var jobs = await context.Jobs
            .AsNoTracking()
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .OrderBy(j => j.JobId)
            .ToArrayAsync();
        var steps = await context.Steps
            .AsNoTracking()
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            .Include(s => (s as JobStep)!.TagFilters)
            .Include(s => s.Dependencies)
            .Include(s => s.DataObjects)
            .Include(s => s.Tags)
            .Include(s => s.ExecutionConditionParameters)
            .OrderBy(s => s.JobId).ThenBy(s => s.StepId)
            .ToArrayAsync();
        var schedules = await context.Schedules
            .AsNoTracking()
            .Include(s => s.Tags)
            .OrderBy(s => s.JobId).ThenBy(s => s.ScheduleId)
        .ToArrayAsync();

        foreach (var job in jobs)
        {
            job.Steps = steps.Where(s => s.JobId == job.JobId).ToArray();
            job.Schedules = schedules.Where(s => s.JobId == job.JobId).ToArray();
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
            .Include(t => t.Lookups)
            .OrderBy(t => t.DataTableId)
            .ToArrayAsync();
        var dataTableCategories = await context.MasterDataTableCategories
            .AsNoTracking()
            .OrderBy(c => c.CategoryId)
            .ToArrayAsync();

        var snapshot = new EnvironmentSnapshot
        {
            Connections = connections,
            AppRegistrations = appRegistrations,
            PipelineClients = pipelineClients,
            FunctionApps = functionApps,
            QlikCloudClients = qlikCloudClients,
            BlobStorageClients = blobStorageClients,
            Jobs = jobs,
            JobCategories = jobCategories,
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
