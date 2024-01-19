namespace Biflow.Ui.Core;

public record DeleteDataObjectCommand(Guid ObjectId) : IRequest;

internal class DeleteDataObjectCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<DeleteDataObjectCommand>
{
    public async Task Handle(DeleteDataObjectCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var dataObject = await context.DataObjects
            .FirstOrDefaultAsync(d => d.ObjectId == request.ObjectId, cancellationToken);
        if (dataObject is not null)
        {
            context.DataObjects.Remove(dataObject);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}