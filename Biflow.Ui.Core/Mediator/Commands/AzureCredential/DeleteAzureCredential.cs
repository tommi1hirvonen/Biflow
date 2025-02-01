namespace Biflow.Ui.Core;

public record DeleteAzureCredentialCommand(Guid AzureCredentialId) : IRequest;

[UsedImplicitly]
internal class DeleteAzureCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteAzureCredentialCommand>
{
    public async Task Handle(DeleteAzureCredentialCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.AzureCredentials
            .FirstOrDefaultAsync(p => p.AzureCredentialId == request.AzureCredentialId, cancellationToken)
            ?? throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        context.AzureCredentials.Remove(client);
        await context.SaveChangesAsync(cancellationToken);
    }
}