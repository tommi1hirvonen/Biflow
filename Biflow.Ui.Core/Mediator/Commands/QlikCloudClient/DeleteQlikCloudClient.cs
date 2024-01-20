namespace Biflow.Ui.Core;

public record DeleteQlikCloudClientCommand(Guid QlikCloudClientId) : IRequest;

internal class DeleteQlikCloudClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteQlikCloudClientCommand>
{
    public async Task Handle(DeleteQlikCloudClientCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.QlikCloudClients
            .FirstOrDefaultAsync(c => c.QlikCloudClientId == request.QlikCloudClientId, cancellationToken);
        if (client is not null)
        {
            context.QlikCloudClients.Remove(client);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}