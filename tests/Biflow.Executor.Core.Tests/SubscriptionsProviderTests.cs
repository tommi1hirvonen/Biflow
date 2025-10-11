using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.Tests;

public class SubscriptionsProviderTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private readonly ISubscriptionsProviderFactory _subscriptionsProviderFactory =
        fixture.Services.GetRequiredService<ISubscriptionsProviderFactory>();
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory =
        fixture.Services.GetRequiredService<IDbContextFactory<ExecutorDbContext>>();

    [Fact]
    public async Task GetJobSubscriptions()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var execution = await context.Executions.FirstAsync();
        var subscriptionsProvider = _subscriptionsProviderFactory.Create(execution);
        var subs = await subscriptionsProvider.GetJobSubscriptionsAsync();
        Assert.NotEmpty(subs);
    }
    
    [Fact]
    public async Task GetTagSubscriptions()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var execution = await context.Executions.FirstAsync();
        var subscriptionsProvider = _subscriptionsProviderFactory.Create(execution);
        var subs = await subscriptionsProvider.GetStepTagSubscriptionsAsync();
        Assert.NotEmpty(subs);
    }

    [Fact]
    public async Task GetJobTagSubscriptions()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var execution = await context.Executions.FirstAsync();
        var subscriptionsProvider = _subscriptionsProviderFactory.Create(execution);
        var subs = await subscriptionsProvider.GetJobStepTagSubscriptionsAsync();
        Assert.NotEmpty(subs);
    }
}