﻿using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class ExecutionBuilderTests(DatabaseFixture fixture)
{
    private readonly IExecutionBuilderFactory<AppDbContext> _executionBuilderFactory = fixture.ExecutionBuilderFactory;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = fixture.DbContextFactory;

    [Fact]
    public async Task TestBuildingExecution()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .FirstAsync(j => j.JobName == "Test job 1");

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
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .FirstAsync(j => j.JobName == "Test job 2");

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
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var schedule = await ctx.Schedules
            .AsNoTracking()
            .FirstAsync(s => s.ScheduleName == "Test schedule 1");

        using var builder = await _executionBuilderFactory.CreateAsync(schedule.JobId, schedule.ScheduleId,
            [
                context => step => step.IsEnabled,
                context => step => !step.StepName!.Contains("Copy"),
                context => step =>
                // Schedule has no tag filters
                !context.Schedules.Any(sch => sch.ScheduleId == schedule.ScheduleId && sch.TagFilter.Any()) ||
                // There's at least one match between the step's tags and the schedule's tags
                step.Tags.Any(t1 => context.Schedules.Any(sch => sch.ScheduleId == schedule.ScheduleId && sch.TagFilter.Any(t2 => t1.TagId == t2.TagId)))
            ]);
        Assert.NotNull(builder);
        builder.AddAll();
        var execution = await builder.SaveExecutionAsync();
        Assert.NotNull(execution);
        Assert.Equal(5, execution.StepExecutions.Count);
    }

    [Fact]
    public async Task TestBuildingExecutionWithScheduleWithTags()
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var schedule = await ctx.Schedules
            .AsNoTracking()
            .FirstAsync(s => s.ScheduleName == "Test schedule 2");

        using var builder = await _executionBuilderFactory.CreateAsync(schedule.JobId, schedule.ScheduleId,
            [
                context => step => step.IsEnabled,
                context => step =>
                // Schedule has no tag filters
                !context.Schedules.Any(sch => sch.ScheduleId == schedule.ScheduleId && sch.TagFilter.Any()) ||
                // There's at least one match between the step's tags and the schedule's tags
                step.Tags.Any(t1 => context.Schedules.Any(sch => sch.ScheduleId == schedule.ScheduleId && sch.TagFilter.Any(t2 => t1.TagId == t2.TagId)))
            ]);
        Assert.NotNull(builder);
        builder.AddAll();
        var execution = await builder.SaveExecutionAsync();
        Assert.NotNull(execution);
        Assert.Single(execution.StepExecutions);
    }
}
