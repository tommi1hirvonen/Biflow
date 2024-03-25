using Biflow.Core;
using Biflow.Core.Entities;
using Biflow.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class SerializationTests(SerializationFixture fixture) : IClassFixture<SerializationFixture>
{
    private static JsonSerializerOptions Options => EnvironmentSnapshot.JsonSerializerOptions;

    [Fact]
    public void Serialize_Connections()
    {
        var json = JsonSerializer.Serialize(fixture.Connections, Options);
        var items = JsonSerializer.Deserialize<ConnectionInfoBase[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.ConnectionId, Guid.Empty));
        Assert.All(items, x => Assert.DoesNotContain("password", x.ConnectionString, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Serialize_AppRegistrations()
    {
        var json = JsonSerializer.Serialize(fixture.AppRegistrations, Options);
        var items = JsonSerializer.Deserialize<AppRegistration[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.AppRegistrationId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.ClientSecret ?? ""));
    }

    [Fact]
    public void Serialize_PipelineClients()
    {
        var json = JsonSerializer.Serialize(fixture.PipelineClients, Options);
        var items = JsonSerializer.Deserialize<PipelineClient[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.PipelineClientId, Guid.Empty));
    }

    [Fact]
    public void Serialize_FunctionApps()
    {
        var json = JsonSerializer.Serialize(fixture.FunctionApps, Options);
        var items = JsonSerializer.Deserialize<FunctionApp[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.FunctionAppId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.FunctionAppKey ?? ""));
    }

    [Fact]
    public void Serialize_QlikCloudClients()
    {
        var json = JsonSerializer.Serialize(fixture.QlikCloudClients, Options);
        var items = JsonSerializer.Deserialize<QlikCloudClient[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.QlikCloudClientId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.ApiToken));
    }

    [Fact]
    public void Serialize_BlobStorageClients()
    {
        var json = JsonSerializer.Serialize(fixture.BlobStorageClients, Options);
        var items = JsonSerializer.Deserialize<BlobStorageClient[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.BlobStorageClientId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.ConnectionString ?? ""));
        Assert.All(items, x => Assert.DoesNotContain("sig=", x.StorageAccountUrl, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Serialize_Jobs()
    {
        var json = JsonSerializer.Serialize(fixture.Jobs, Options);
        var items = JsonSerializer.Deserialize<Job[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.JobId, Guid.Empty));
    }

    [Fact]
    public void Serialize_Steps()
    {
        var json = JsonSerializer.Serialize(fixture.Steps, Options);
        var items = JsonSerializer.Deserialize<Step[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.StepId, Guid.Empty));
        Assert.All(items, x => Assert.NotEqual(x.JobId, Guid.Empty));
        var functionSteps = items.OfType<FunctionStep>();
        Assert.All(functionSteps, x => Assert.Empty(x.FunctionKey ?? ""));
    }

    [Fact]
    public void Serialize_Tags()
    {
        var json = JsonSerializer.Serialize(fixture.Tags, Options);
        var items = JsonSerializer.Deserialize<StepTag[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.TagId, Guid.Empty));
    }

    [Fact]
    public void Serialize_DataObjects()
    {
        var json = JsonSerializer.Serialize(fixture.DataObjects, Options);
        var items = JsonSerializer.Deserialize<DataObject[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.ObjectId, Guid.Empty));
    }

    [Fact]
    public void Serialize_DataTables()
    {
        var json = JsonSerializer.Serialize(fixture.DataTables, Options);
        var items = JsonSerializer.Deserialize<MasterDataTable[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.DataTableId, Guid.Empty));
        Assert.All(items.SelectMany(i => i.Lookups), x => Assert.NotEqual(x.LookupId, Guid.Empty));
    }

    [Fact]
    public void Serialize_DataTableCategories()
    {
        var json = JsonSerializer.Serialize(fixture.DataTableCategories, Options);
        var items = JsonSerializer.Deserialize<MasterDataTableCategory[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.CategoryId, Guid.Empty));
    }

    [Fact]
    public void Serialize_Snapshot()
    {
        var snapshot = new EnvironmentSnapshot
        {
            Connections = fixture.Connections,
            AppRegistrations = fixture.AppRegistrations,
            PipelineClients = fixture.PipelineClients,
            FunctionApps = fixture.FunctionApps,
            QlikCloudClients = fixture.QlikCloudClients,
            BlobStorageClients = fixture.BlobStorageClients,
            Jobs = fixture.Jobs,
            Tags = fixture.Tags,
            DataObjects = fixture.DataObjects,
            DataTables = fixture.DataTables,
            DataTableCategories = fixture.DataTableCategories
        };
        var json = JsonSerializer.Serialize(snapshot, Options);
        var items = JsonSerializer.Deserialize<EnvironmentSnapshot>(json, Options);
        Assert.NotNull(items);
    }
}

public class SerializationFixture(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly IDbContextFactory<AppDbContext> dbContextFactory = fixture.DbContextFactory;

    public ConnectionInfoBase[] Connections { get; private set; } = [];
    public AppRegistration[] AppRegistrations { get; private set; } = [];
    public PipelineClient[] PipelineClients { get; private set; } = [];
    public FunctionApp[] FunctionApps { get; private set; } = [];
    public QlikCloudClient[] QlikCloudClients { get; private set; } = [];
    public BlobStorageClient[] BlobStorageClients { get; private set; } = [];
    
    public Job[] Jobs { get; private set; } = [];
    public Step[] Steps { get; private set; } = [];
    public Schedule[] Schedules { get; private set; } = [];

    public StepTag[] Tags { get; private set; } = [];
    public DataObject[] DataObjects {  get; private set; } = [];

    public MasterDataTable[] DataTables { get; private set; } = [];
    public MasterDataTableCategory[] DataTableCategories { get; private set; } = [];

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        using var context = await dbContextFactory.CreateDbContextAsync();

        Connections = await context.Connections
            .AsNoTracking()
            .OrderBy(c => c.ConnectionId)
            .ToArrayAsync();
        AppRegistrations = await context.AppRegistrations
            .AsNoTracking()
            .OrderBy(a => a.AppRegistrationId)
            .ToArrayAsync();
        PipelineClients = await context.PipelineClients
            .AsNoTracking()
            .OrderBy(p => p.PipelineClientId)
            .ToArrayAsync();
        FunctionApps = await context.FunctionApps
            .AsNoTracking()
            .OrderBy(f => f.FunctionAppId)
            .ToArrayAsync();
        QlikCloudClients = await context.QlikCloudClients
            .AsNoTracking()
            .OrderBy(q => q.QlikCloudClientId)
            .ToArrayAsync();
        BlobStorageClients = await context.BlobStorageClients
            .AsNoTracking()
            .OrderBy(b => b.BlobStorageClientId)
            .ToArrayAsync();


        Jobs = await context.Jobs
            .AsNoTracking()
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .OrderBy(j => j.JobId)
            .ToArrayAsync();
        Steps = await context.Steps
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
        Schedules = await context.Schedules
            .AsNoTracking()
            .Include(s => s.Tags)
            .OrderBy(s => s.JobId).ThenBy(s => s.ScheduleId)
            .ToArrayAsync();

        foreach (var job in Jobs)
        {
            job.Steps.AddRange(Steps.Where(s => s.JobId == job.JobId));
            job.Schedules.AddRange(Schedules.Where(s => s.JobId == job.JobId));
        }

        Tags = await context.Tags
            .AsNoTracking()
            .OrderBy(t => t.TagId)
            .ToArrayAsync();
        DataObjects = await context.DataObjects
            .AsNoTracking()
            .OrderBy(d => d.ObjectId)
            .ToArrayAsync();

        DataTables = await context.MasterDataTables
            .AsNoTracking()
            .Include(t => t.Lookups)
            .OrderBy(t => t.DataTableId)
            .ToArrayAsync();
        DataTableCategories = await context.MasterDataTableCategories
            .AsNoTracking()
            .OrderBy(c => c.CategoryId)
            .ToArrayAsync();
    }
}