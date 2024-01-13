namespace Biflow.Ui.Core;

public record DeleteBlobStorageClientCommand(Guid BlobStorageClientId) : IRequest;

internal class DeleteBlobStorageClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteBlobStorageClientCommand>
{
    public async Task Handle(DeleteBlobStorageClientCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.BlobStorageClients
            .FirstOrDefaultAsync(p => p.BlobStorageClientId == request.BlobStorageClientId, cancellationToken);
        if (client is not null)
        {
            context.BlobStorageClients.Remove(client);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}