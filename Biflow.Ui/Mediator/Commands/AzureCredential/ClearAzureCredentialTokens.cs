namespace Biflow.Ui;

public record ClearAzureCredentialTokensCommand(Guid AzureCredentialId) : IRequest;

internal class ClearAzureCredentialTokensCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory, ITokenService tokenService, IExecutorService executorService)
    : IRequestHandler<ClearAzureCredentialTokensCommand>
{
    public async Task Handle(ClearAzureCredentialTokensCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tokens = await context.AccessTokens
            .Where(t => t.AzureCredentialId == request.AzureCredentialId)
            .ToArrayAsync(cancellationToken);
        foreach (var token in tokens)
        {
            context.AccessTokens.Remove(token);
        }
        await context.SaveChangesAsync(cancellationToken);
        await tokenService.ClearAsync(request.AzureCredentialId, cancellationToken);
        await executorService.ClearTokenCacheAsync(request.AzureCredentialId, cancellationToken);
    }
}
