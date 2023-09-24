using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Executor.Core.Notification;

internal class SubscriptionsProviderFactory : ISubscriptionsProviderFactory
{
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;

    public SubscriptionsProviderFactory(IDbContextFactory<ExecutorDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public ISubscriptionsProvider Create(Execution execution) => new SubscriptionsProvider(_dbContextFactory, execution);
}

public interface ISubscriptionsProviderFactory
{
    public ISubscriptionsProvider Create(Execution execution);
}