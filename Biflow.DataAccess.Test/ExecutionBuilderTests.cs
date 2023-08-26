using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class ExecutionBuilderTests
{
    private readonly IExecutionBuilderFactory _executionBuilderFactory;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    public ExecutionBuilderTests(DatabaseFixture fixture)
    {
        _executionBuilderFactory = fixture.ExecutionBuilderFactory;
        _dbContextFactory = fixture.DbContextFactory;
    }

    [Fact]
    public async Task TestBuildingExecution()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .FirstAsync(j => j.JobName == "Test job");

        var builder = await _executionBuilderFactory.CreateAsync(job.JobId, "testuser");
        foreach (var step in builder.Steps)
        {
            builder.AddStep(step);
        }
        await builder.SaveAsync();
    }

    [Fact]
    public async Task TestBuildingExecutionWithJobStep()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .FirstAsync(j => j.JobName == "Another job");

        var builder = await _executionBuilderFactory.CreateAsync(job.JobId, "testuser");
        foreach (var step in builder.Steps)
        {
            builder.AddStep(step);
        }
        await builder.SaveAsync();
    }
}
