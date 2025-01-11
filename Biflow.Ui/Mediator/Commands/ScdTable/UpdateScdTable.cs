namespace Biflow.Ui;

public record UpdateScdTableCommand(ScdTable Table) : IRequest;

public class UpdateScdTableCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateScdTableCommand>
{
    public async Task Handle(UpdateScdTableCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Table).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}