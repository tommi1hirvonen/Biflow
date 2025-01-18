namespace Biflow.Ui.Core;

public record DeleteDataObjectCommand(Guid ObjectId) : IRequest;

[UsedImplicitly]
internal class DeleteDataObjectCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteDataObjectCommand>
{
    public async Task Handle(DeleteDataObjectCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var dataObject = await context.DataObjects
            .FirstOrDefaultAsync(d => d.ObjectId == request.ObjectId, cancellationToken)
            ?? throw new NotFoundException<DataObject>(request.ObjectId);
        context.DataObjects.Remove(dataObject);
        await context.SaveChangesAsync(cancellationToken);
    }
}