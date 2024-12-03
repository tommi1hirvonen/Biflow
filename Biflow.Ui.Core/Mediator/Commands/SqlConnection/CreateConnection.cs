namespace Biflow.Ui.Core;

public record CreateSqlConnectionCommand(SqlConnectionBase Connection) : IRequest;

internal class CreateSqlConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<CreateSqlConnectionCommand>
{
    public async Task Handle(CreateSqlConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.SqlConnections.Add(request.Connection);
        await context.SaveChangesAsync(cancellationToken);
    }
}