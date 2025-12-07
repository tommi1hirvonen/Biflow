namespace Biflow.Ui.Core;

/// <summary>
/// 
/// </summary>
/// <param name="ProxyId"></param>
/// <param name="ProxyName"></param>
/// <param name="ProxyUrl"></param>
/// <param name="ApiKey">pass null to retain the previous ApiKey value, pass empty string to clear ApiKey value</param>
public record UpdateProxyCommand(
    Guid ProxyId,
    string ProxyName,
    string ProxyUrl,
    string? ApiKey,
    int MaxConcurrentExeSteps) : IRequest<Proxy>;

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
        proxy.MaxConcurrentExeSteps = request.MaxConcurrentExeSteps;
        if (request.ApiKey is { Length: 0 })
        {
            proxy.ApiKey = null;
        }
        else if (request.ApiKey is not null)
        {
            proxy.ApiKey = request.ApiKey;
        }
        proxy.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return proxy;
    }
}

