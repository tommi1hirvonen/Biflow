namespace Biflow.Ui.Core;

public record CreateScdTableCommand(ScdTable Table) : IRequest;

public class CreateScdTableCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateScdTableCommand>
{
    public async Task Handle(CreateScdTableCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.ScdTables.Add(request.Table);
        await context.SaveChangesAsync(cancellationToken);
    }
}