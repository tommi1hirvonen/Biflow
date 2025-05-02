namespace Biflow.Ui.Core;

public record UpdateProxyCommand(Guid ProxyId, string ProxyName, string ProxyUrl, string? ApiKey) : IRequest<Proxy>;

[UsedImplicitly]
internal class UpdateProxyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateProxyCommand, Proxy>
{
    public async Task<Proxy> Handle(UpdateProxyCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var proxy = await dbContext.Proxies
            .FirstOrDefaultAsync(x => x.ProxyId == request.ProxyId, cancellationToken)
            ?? throw new NotFoundException<Proxy>(request.ProxyId);
            
        proxy.ProxyName = request.ProxyName;
        proxy.ProxyUrl = request.ProxyUrl;
        if (request.ApiKey is not null)
        {
            proxy.ApiKey = request.ApiKey;
        }
        proxy.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return proxy;
    }
}

