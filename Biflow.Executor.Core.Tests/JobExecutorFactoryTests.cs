using Biflow.Executor.Core.JobExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.Test;
public class JobExecutorFactoryTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private readonly IJobExecutorFactory _jobExecutorFactory =
        fixture.Services.GetRequiredService<IJobExecutorFactory>();
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory =
        fixture.Services.GetRequiredService<IDbContextFactory<ExecutorDbContext>>();

    [Fact]
    public async Task CreateJobExecutor()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var execution = await context.Executions.FirstAsync();
        var jobExecutor = await _jobExecutorFactory.CreateAsync(execution.ExecutionId);
    }
}
