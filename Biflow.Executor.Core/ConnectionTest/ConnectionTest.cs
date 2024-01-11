using Microsoft.EntityFrameworkCore;

namespace Biflow.Executor.Core.ConnectionTest;

internal class ConnectionTest(IDbContextFactory<ExecutorDbContext> dbContextFactory) : IConnectionTest
{
    public async Task RunAsync()
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        await context.Database.OpenConnectionAsync();
        await context.Database.CloseConnectionAsync();
    }
}
