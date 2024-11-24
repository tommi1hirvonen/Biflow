namespace Biflow.Ui.Core;

public record CreateConnectionCommand(ConnectionBase Connection) : IRequest;

internal class CreateConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<CreateConnectionCommand>
{
    public async Task Handle(CreateConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Connections.Add(request.Connection);
        await context.SaveChangesAsync(cancellationToken);
    }
}