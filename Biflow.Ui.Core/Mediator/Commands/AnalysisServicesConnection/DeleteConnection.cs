namespace Biflow.Ui.Core;

public record DeleteAnalysisServicesConnectionCommand(Guid ConnectionId) : IRequest;

internal class DeleteAnalysisServicesConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteAnalysisServicesConnectionCommand>
{
    public async Task Handle(DeleteAnalysisServicesConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var connection = await context.AnalysisServicesConnections
            .FirstOrDefaultAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken);
        if (connection is not null)
        {
            context.AnalysisServicesConnections.Remove(connection);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}