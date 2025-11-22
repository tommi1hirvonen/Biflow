using JetBrains.Annotations;

namespace Biflow.Ui.Mediator.Commands.DataObject;

internal record DeleteUnusedDataObjectsCommand : IRequest<DeleteUnusedDataObjectsResponse>;

internal record DeleteUnusedDataObjectsResponse(IEnumerable<Biflow.Core.Entities.DataObject> DeletedDataObjects);

[UsedImplicitly]
internal class DeleteUnusedDataObjectsCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteUnusedDataObjectsCommand, DeleteUnusedDataObjectsResponse>
{
    public async Task<DeleteUnusedDataObjectsResponse> Handle(DeleteUnusedDataObjectsCommand request,
        CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var unused = await context.DataObjects
            .Where(d => !d.Steps.Any())
            .ToArrayAsync(cancellationToken);
        context.DataObjects.RemoveRange(unused);
        await context.SaveChangesAsync(cancellationToken);
        return new DeleteUnusedDataObjectsResponse(unused);
    }
}

