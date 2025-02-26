namespace Biflow.Ui.Core;

public record CreateApiKeyCommand(
    string Name,
    string? KeyValue,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    string[] Scopes) : IRequest<ApiKey>;

[UsedImplicitly]
internal class CreateApiKeyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateApiKeyCommand, ApiKey>
{
    public async Task<ApiKey> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var apiKey = new ApiKey(request.KeyValue)
        {
            Name = request.Name,
            IsRevoked = false,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            Scopes = request.Scopes.Order().ToList()
        };
        dbContext.ApiKeys.Add(apiKey);
        await dbContext.SaveChangesAsync(cancellationToken);
        return apiKey;
    }
}