namespace Biflow.Ui;

public record CreateBlobStorageClientCommand(BlobStorageClient Client) : IRequest;

internal class CreateBlobStorageClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateBlobStorageClientCommand>
{
    public async Task Handle(CreateBlobStorageClientCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.BlobStorageClients.Add(request.Client);
        if (request.Client.AzureCredential is not null)
        {
            context.Entry(request.Client.AzureCredential).State = EntityState.Unchanged;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}