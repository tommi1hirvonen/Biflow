namespace Biflow.Ui;

public record DeleteAzureCredentialCommand(Guid AzureCredentialId) : IRequest;

internal class DeleteAzureCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteAzureCredentialCommand>
{
    public async Task Handle(DeleteAzureCredentialCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.AzureCredentials
            .FirstOrDefaultAsync(p => p.AzureCredentialId == request.AzureCredentialId, cancellationToken);
        if (client is not null)
        {
            context.AzureCredentials.Remove(client);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}