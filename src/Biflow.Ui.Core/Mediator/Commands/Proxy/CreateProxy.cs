namespace Biflow.Ui.Core;

public record CreateProxyCommand(
    string ProxyName,
    string ProxyUrl,
    string? ApiKey,
    int MaxConcurrentExeSteps) : IRequest<Proxy>;

[UsedImplicitly]
internal class CreateProxyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateProxyCommand, Proxy>
{
    public async Task<Proxy> Handle(CreateProxyCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var proxy = new Proxy
        {
            ProxyName = request.ProxyName,
            ProxyUrl = request.ProxyUrl,
            ApiKey = request.ApiKey,
            MaxConcurrentExeSteps = request.MaxConcurrentExeSteps
        };
        proxy.EnsureDataAnnotationsValidated();
        dbContext.Proxies.Add(proxy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return proxy;
    }
}

