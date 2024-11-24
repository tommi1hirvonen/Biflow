namespace Biflow.Ui.Core;

public record DeleteConnectionCommand(Guid ConnectionId) : IRequest;

internal class DeleteConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteConnectionCommand>
{
    public async Task Handle(DeleteConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var connection = await context.Connections
            .FirstOrDefaultAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken);
        if (connection is not null)
        {
            context.Connections.Remove(connection);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}