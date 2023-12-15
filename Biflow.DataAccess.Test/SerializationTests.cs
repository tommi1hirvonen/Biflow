using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class SerializationTests(SerializationFixture fixture) : IClassFixture<SerializationFixture>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

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
    public void Serialize_AppRegistrations()
    {
        var json = JsonSerializer.Serialize(fixture.AppRegistrations, Options);
        var items = JsonSerializer.Deserialize<AppRegistration[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.AppRegistrationId, Guid.Empty));
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
    }

    [Fact]
    public void Serialize_QlikCloudClients()
    {
        var json = JsonSerializer.Serialize(fixture.QlikCloudClients, Options);
        var items = JsonSerializer.Deserialize<QlikCloudClient[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.QlikCloudClientId, Guid.Empty));
    }

    [Fact]
    public void Serialize_BlobStorageClients()
    {
        var json = JsonSerializer.Serialize(fixture.BlobStorageClients, Options);
        var items = JsonSerializer.Deserialize<BlobStorageClient[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.BlobStorageClientId, Guid.Empty));
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
}

public class SerializationFixture(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly IDbContextFactory<AppDbContext> dbContextFactory = fixture.DbContextFactory;

    public Job[] Jobs { get; private set; } = [];
    public JobCategory[] JobCategories { get; private set; } = [];
    public Step[] Steps {  get; private set; } = [];
    public AppRegistration[] AppRegistrations { get; private set; } = [];
    public PipelineClient[] PipelineClients { get; private set; } = [];
    public FunctionApp[] FunctionApps { get; private set; } = [];
    public QlikCloudClient[] QlikCloudClients { get; private set; } = [];
    public BlobStorageClient[] BlobStorageClients { get; private set; } = [];
    public Tag[] Tags { get; private set; } = [];

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        Jobs = await context.Jobs
            .AsNoTracking()
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .Include(j => j.Schedules).ThenInclude(s => s.Tags)
            .ToArrayAsync();
        JobCategories = await context.JobCategories
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
        Steps = await context.Steps
            .AsNoTracking()
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            .Include(s => (s as JobStep)!.TagFilters)
            .Include(s => s.Dependencies)
            .Include(s => s.DataObjects)
            .ThenInclude(t => t.DataObject)
            .Include(s => s.Tags)
            .Include(s => s.ExecutionConditionParameters)
            .ToArrayAsync();
        Tags = await context.Tags
            .AsNoTracking()
            .ToArrayAsync();
    }
}