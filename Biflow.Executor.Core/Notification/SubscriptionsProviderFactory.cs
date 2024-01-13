namespace Biflow.Executor.Core.Notification;

internal class SubscriptionsProviderFactory(IDbContextFactory<ExecutorDbContext> dbContextFactory) : ISubscriptionsProviderFactory
{
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    public ISubscriptionsProvider Create(Execution execution) => new SubscriptionsProvider(_dbContextFactory, execution);
}

public interface ISubscriptionsProviderFactory
{
    public ISubscriptionsProvider Create(Execution execution);
}