namespace Biflow.Ui.Core;

public record CreateBlobStorageClientCommand(BlobStorageClient Client) : IRequest;

internal class CreateBlobStorageClientCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<CreateBlobStorageClientCommand>
{
    public async Task Handle(CreateBlobStorageClientCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.BlobStorageClients.Add(request.Client);
        await context.SaveChangesAsync(cancellationToken);
    }
}