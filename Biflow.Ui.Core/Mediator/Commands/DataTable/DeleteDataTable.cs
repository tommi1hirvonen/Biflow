namespace Biflow.Ui.Core;

public record DeleteDataTableCommand(Guid DataTableId) : IRequest;

[UsedImplicitly]
internal class DeleteDataTableCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteDataTableCommand>
{
    public async Task Handle(DeleteDataTableCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var table = await context.MasterDataTables
            .IgnoreQueryFilters()
            .Include(t => t.Lookups)
            .Include(t => t.DependentLookups)
            .FirstOrDefaultAsync(t => t.DataTableId == request.DataTableId, cancellationToken)
            ?? throw new NotFoundException<MasterDataTable>(request.DataTableId);
        context.MasterDataTables.Remove(table);
        await context.SaveChangesAsync(cancellationToken);
    }
}