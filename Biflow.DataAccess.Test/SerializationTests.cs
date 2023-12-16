using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class SerializationTests(SerializationFixture fixture) : IClassFixture<SerializationFixture>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { JsonSensitiveAttribute.SensitiveModifier }
        }
    };

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
    public void Serialize_JobCategories()
    {
        var json = JsonSerializer.Serialize(fixture.JobCategories, Options);
        var items = JsonSerializer.Deserialize<JobCategory[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.CategoryId, Guid.Empty));
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
        var items = JsonSerializer.Deserialize<Tag[]>(json, Options);
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
        var snapshot = new Snapshot
        {
            Connections = fixture.Connections,
            AppRegistrations = fixture.AppRegistrations,
            PipelineClients = fixture.PipelineClients,
            FunctionApps = fixture.FunctionApps,
            QlikCloudClients = fixture.QlikCloudClients,
            BlobStorageClients = fixture.BlobStorageClients,
            Jobs = fixture.Jobs,
            JobCategories = fixture.JobCategories,
            Steps = fixture.Steps,
            Tags = fixture.Tags,
            DataObjects = fixture.DataObjects,
            DataTables = fixture.DataTables,
            DataTableCategories = fixture.DataTableCategories
        };
        var json = JsonSerializer.Serialize(snapshot, Options);
        var items = JsonSerializer.Deserialize<Snapshot>(json, Options);
        Assert.NotNull(items);
    }
}

file class Snapshot
{
    public required ConnectionInfoBase[] Connections { get; init; }
    public required AppRegistration[] AppRegistrations { get; init; }
    public required PipelineClient[] PipelineClients { get; init; }
    public required FunctionApp[] FunctionApps { get; init; }
    public required QlikCloudClient[] QlikCloudClients { get; init; }
    public required BlobStorageClient[] BlobStorageClients { get; init; }
    public required Job[] Jobs { get; init; }
    public required JobCategory[] JobCategories { get; init; }
    public required Step[] Steps { get; init; }
    public required Tag[] Tags { get; init; }
    public required DataObject[] DataObjects { get; init; }
    public required MasterDataTable[] DataTables { get; init; }
    public required MasterDataTableCategory[] DataTableCategories { get; init; }
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
    public JobCategory[] JobCategories { get; private set; } = [];
    public Step[] Steps { get; private set; } = [];

    public Tag[] Tags { get; private set; } = [];
    public DataObject[] DataObjects {  get; private set; } = [];

    public MasterDataTable[] DataTables { get; private set; } = [];
    public MasterDataTableCategory[] DataTableCategories { get; private set; } = [];

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        using var context = await dbContextFactory.CreateDbContextAsync();

        Connections = await context.Connections
            .AsNoTracking()
            .ToArrayAsync();
        AppRegistrations = await context.AppRegistrations
            .AsNoTracking()
            .ToArrayAsync();
        PipelineClients = await context.PipelineClients
            .AsNoTracking()
            .ToArrayAsync();
        FunctionApps = await context.FunctionApps
            .AsNoTracking()
            .ToArrayAsync();
        QlikCloudClients = await context.QlikCloudClients
            .AsNoTracking()
            .ToArrayAsync();
        BlobStorageClients = await context.BlobStorageClients
            .AsNoTracking()
            .ToArrayAsync();

        Jobs = await context.Jobs
            .AsNoTracking()
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .Include(j => j.Schedules).ThenInclude(s => s.Tags)
            .ToArrayAsync();
        JobCategories = await context.JobCategories
            .AsNoTracking()
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
            .ToArrayAsync();

        Tags = await context.Tags
            .AsNoTracking()
            .ToArrayAsync();
        DataObjects = await context.DataObjects
            .AsNoTracking()
            .ToArrayAsync();

        DataTables = await context.MasterDataTables
            .AsNoTracking()
            .Include(t => t.Lookups)
            .ToArrayAsync();
        DataTableCategories = await context.MasterDataTableCategories
            .AsNoTracking()
            .ToArrayAsync();
    }
}