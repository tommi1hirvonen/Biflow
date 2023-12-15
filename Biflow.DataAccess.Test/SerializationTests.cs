using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class SerializationTests(SerializationFixture fixture) : IClassFixture<SerializationFixture>
{
    private readonly Job job = fixture.Job;

    [Fact]
    public void JsonSerialize()
    {
        var _ = JsonSerializer.Serialize(job);
    }
}

public class SerializationFixture(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly IDbContextFactory<AppDbContext> dbContextFactory = fixture.DbContextFactory;

    public Job Job { get; private set; } = null!;

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        Job = await context.Jobs
            //.Include(j => j.Category)
            //.Include(j => j.JobParameters)
            //.Include(j => j.JobConcurrencies)
            //.Include($"{nameof(Job.Steps)}.{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            //.Include($"{nameof(Job.Steps)}.{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            //.Include($"{nameof(Job.Steps)}.{nameof(JobStep.TagFilters)}")
            //.Include($"{nameof(Job.Steps)}.{nameof(Step.Dependencies)}")
            //.Include($"{nameof(Job.Steps)}.{nameof(Step.DataObjects)}.{nameof(StepDataObject.DataObject)}")
            //.Include($"{nameof(Job.Steps)}.{nameof(Step.Tags)}")
            //.Include($"{nameof(Job.Steps)}.{nameof(Step.ExecutionConditionParameters)}")
            .FirstAsync(j => j.JobName == "Test job");
    }
}