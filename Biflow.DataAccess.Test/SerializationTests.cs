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
    public void SerializeJobs()
    {
        var json = JsonSerializer.Serialize(fixture.Jobs, Options);
        var _ = JsonSerializer.Deserialize<Job[]>(json, Options);
    }

    [Fact]
    public void SerializeJobCategories()
    {
        var json = JsonSerializer.Serialize(fixture.JobCategories, Options);
        var _ = JsonSerializer.Deserialize<JobCategory[]>(json, Options);
    }
}

public class SerializationFixture(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly IDbContextFactory<AppDbContext> dbContextFactory = fixture.DbContextFactory;

    public Job[] Jobs { get; private set; } = [];
    public JobCategory[] JobCategories { get; private set; } = [];
    public Step[] Steps {  get; private set; } = [];

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        Jobs = await context.Jobs
            .AsNoTracking()
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
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
            .ThenInclude(t => t.DataObject)
            .Include(s => s.Tags)
            .Include(s => s.ExecutionConditionParameters)
            .ToArrayAsync();
    }
}