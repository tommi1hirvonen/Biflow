using Biflow.DataAccess.Models;
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
        Assert.NotNull(builder);
        foreach (var step in builder.Steps)
        {
            step.AddToExecution();
        }
        var execution = await builder.SaveExecutionAsync();
        Assert.NotNull(execution);
    }

    [Fact]
    public async Task TestBuildingExecutionWithJobStep()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .FirstAsync(j => j.JobName == "Another job");

        var builder = await _executionBuilderFactory.CreateAsync(job.JobId, "testuser");
        Assert.NotNull(builder);
        foreach (var step in builder.Steps)
        {
            step.AddToExecution();
        }
        var execution = await builder.SaveExecutionAsync();
        Assert.NotNull(execution);
    }

    [Fact]
    public async Task TestBuildingExecutionWithSchedule()
    {
        using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var schedule = await ctx.Schedules
            .AsNoTracking()
            .FirstAsync(s => s.ScheduleName == "Test schedule");

        var builder = await _executionBuilderFactory.CreateAsync(schedule.JobId, schedule.ScheduleId,
            context => step => step.IsEnabled,
            context => step =>
            // Schedule has no tag filters
            !context.Schedules.Any(sch => sch.ScheduleId == schedule.ScheduleId && sch.Tags.Any()) ||
            // There's at least one match between the step's tags and the schedule's tags
            step.Tags.Any(t1 => context.Schedules.Any(sch => sch.ScheduleId == schedule.ScheduleId && sch.Tags.Any(t2 => t1.TagId == t2.TagId))));
        Assert.NotNull(builder);
        builder.AddAll();
        var execution = await builder.SaveExecutionAsync();
        Assert.NotNull(execution);
        Assert.Equal(4, execution.StepExecutions.Count);
    }

    [Fact]
    public async Task TestBuildingExecutionWithScheduleWithTags()
    {
        using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var schedule = await ctx.Schedules
            .AsNoTracking()
            .FirstAsync(s => s.ScheduleName == "Another schedule");

        var builder = await _executionBuilderFactory.CreateAsync(schedule.JobId, schedule.ScheduleId,
            context => step => step.IsEnabled,
            context => step =>
            // Schedule has no tag filters
            !context.Schedules.Any(sch => sch.ScheduleId == schedule.ScheduleId && sch.Tags.Any()) ||
            // There's at least one match between the step's tags and the schedule's tags
            step.Tags.Any(t1 => context.Schedules.Any(sch => sch.ScheduleId == schedule.ScheduleId && sch.Tags.Any(t2 => t1.TagId == t2.TagId))));
        Assert.NotNull(builder);
        builder.AddAll();
        var execution = await builder.SaveExecutionAsync();
        Assert.NotNull(execution);
        Assert.Equal(1, execution.StepExecutions.Count);
    }
}
