namespace Biflow.Ui.Core;

public record DeleteApiKeyCommand(Guid ApiKeyId) : IRequest;

[UsedImplicitly]
internal class DeleteApiKeyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteApiKeyCommand>
{
    public async Task Handle(DeleteApiKeyCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var key = await context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.ApiKeyId, cancellationToken)
            ?? throw new NotFoundException<ApiKey>(request.ApiKeyId);
        context.ApiKeys.Remove(key);
        await context.SaveChangesAsync(cancellationToken);
    }
}
