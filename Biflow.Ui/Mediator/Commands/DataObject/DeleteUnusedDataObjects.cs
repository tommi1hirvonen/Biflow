namespace Biflow.Ui;

public record DeleteUnusedDataObjectsCommand : IRequest<DeleteUnusedDataObjectsResponse>;

public record DeleteUnusedDataObjectsResponse(IEnumerable<DataObject> DeletedDataObjects);

internal class DeleteUnusedDataObjectsCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteUnusedDataObjectsCommand, DeleteUnusedDataObjectsResponse>
{
    public async Task<DeleteUnusedDataObjectsResponse> Handle(DeleteUnusedDataObjectsCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var unused = await context.DataObjects
            .Where(d => !d.Steps.Any())
            .ToArrayAsync(cancellationToken);
        context.DataObjects.RemoveRange(unused);
        await context.SaveChangesAsync(cancellationToken);
        return new(unused);
    }
}

