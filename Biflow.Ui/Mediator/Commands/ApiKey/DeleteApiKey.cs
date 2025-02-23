using JetBrains.Annotations;

namespace Biflow.Ui;

public record DeleteApiKeyCommand(Guid ApiKeyId) : IRequest;

[UsedImplicitly]
internal class DeleteApiKeyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteApiKeyCommand>
{
    public async Task Handle(DeleteApiKeyCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var key = await context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.ApiKeyId, cancellationToken);
        if (key is not null)
        {
            context.ApiKeys.Remove(key);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
