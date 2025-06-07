namespace Biflow.Ui.Core;

public record DeleteBlobStorageClientCommand(Guid BlobStorageClientId) : IRequest;

[UsedImplicitly]
internal class DeleteBlobStorageClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteBlobStorageClientCommand>
{
    public async Task Handle(DeleteBlobStorageClientCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.BlobStorageClients
            .FirstOrDefaultAsync(p => p.BlobStorageClientId == request.BlobStorageClientId, cancellationToken)
            ?? throw new NotFoundException<BlobStorageClient>(request.BlobStorageClientId);
        context.BlobStorageClients.Remove(client);
        await context.SaveChangesAsync(cancellationToken);
    }
}