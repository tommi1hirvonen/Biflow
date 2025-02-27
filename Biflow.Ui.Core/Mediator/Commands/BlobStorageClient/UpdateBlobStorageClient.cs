namespace Biflow.Ui.Core;

public record UpdateBlobStorageClientCommand(BlobStorageClient Client) : IRequest;

[UsedImplicitly]
internal class UpdateBlobStorageClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateBlobStorageClientCommand>
{
    public async Task Handle(UpdateBlobStorageClientCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.BlobStorageClients
            .FirstOrDefaultAsync(c => c.BlobStorageClientId == request.Client.BlobStorageClientId, cancellationToken)
            ?? throw new NotFoundException<BlobStorageClient>(request.Client.BlobStorageClientId);
        context.Entry(client).CurrentValues.SetValues(request.Client);
        await context.SaveChangesAsync(cancellationToken);
    }
}