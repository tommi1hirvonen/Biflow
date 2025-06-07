namespace Biflow.Ui.Core;

public record DeleteProxyCommand(Guid ProxyId) : IRequest;

[UsedImplicitly]
internal class DeleteProxyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteProxyCommand>
{
    public async Task Handle(DeleteProxyCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var proxy = await dbContext.Proxies
            .FirstOrDefaultAsync(p => p.ProxyId == request.ProxyId, cancellationToken)
            ?? throw new NotFoundException<Proxy>(request.ProxyId);
        dbContext.Proxies.Remove(proxy);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

