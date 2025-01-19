namespace Biflow.Ui.Core;

public record DeleteScdTableCommand(Guid ScdTableId) : IRequest;

[UsedImplicitly]
internal class DeleteScdTableCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteScdTableCommand> 
{
    public async Task Handle(DeleteScdTableCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var table = await context.ScdTables
            .FirstOrDefaultAsync(t => t.ScdTableId == request.ScdTableId, cancellationToken);
        if (table is not null)
        {
            context.ScdTables.Remove(table);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}