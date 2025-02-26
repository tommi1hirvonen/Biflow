namespace Biflow.Ui.Core;

public record UpdateApiKeyCommand(
    Guid ApiKeyId,
    string Name,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    bool IsRevoked,
    string[] Scopes) : IRequest<ApiKey>;

[UsedImplicitly]
internal class UpdateApiKeyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateApiKeyCommand, ApiKey>
{
    public async Task<ApiKey> Handle(UpdateApiKeyCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var key = await context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.ApiKeyId, cancellationToken)
            ?? throw new NotFoundException<ApiKey>(request.ApiKeyId);
        key.Name = request.Name;
        key.ValidFrom = request.ValidFrom;
        key.ValidTo = request.ValidTo;
        key.IsRevoked = request.IsRevoked;
        key.Scopes.Clear();
        key.Scopes.AddRange(request.Scopes);
        key.Scopes.Sort();
        await context.SaveChangesAsync(cancellationToken);
        return key;
    }
}