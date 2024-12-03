namespace Biflow.Ui.Core;

public record UpdateSqlConnectionCommand(SqlConnectionBase Connection) : IRequest;

internal class UpdateSqlConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateSqlConnectionCommand>
{
    public async Task Handle(UpdateSqlConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Connection).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}