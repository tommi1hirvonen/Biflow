namespace Biflow.Ui.Core;

public record UpdateBlobStorageClientCommand(BlobStorageClient Client) : IRequest;

internal class UpdateBlobStorageClientCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<UpdateBlobStorageClientCommand>
{
    public async Task Handle(UpdateBlobStorageClientCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Client).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}