namespace Biflow.Ui;

public record DeleteSqlConnectionCommand(Guid ConnectionId) : IRequest;

internal class DeleteSqlConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteSqlConnectionCommand>
{
    public async Task Handle(DeleteSqlConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var connection = await context.SqlConnections
            .FirstOrDefaultAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken);
        if (connection is not null)
        {
            context.SqlConnections.Remove(connection);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}